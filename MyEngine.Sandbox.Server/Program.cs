using System.Diagnostics;
using LiteNetLib;
using LiteNetLib.Utils;
using MyEngine.Core;
using MyEngine.ECS;
using MyEngine.ECS.Components;
using MyEngine.ECS.Systems;
using MyEngine.Networking;
using MyEngine.Physics;
using MyEngine.Scene;
using Silk.NET.Maths;

string assetsDir = Path.Combine(AppContext.BaseDirectory, "Assets");
var world = new World();

// No IRenderResourceResolver here: entities get RenderInfo (plain names)
// but never a GPU-backed Render component. This process never touches OpenGL.
SceneLoader.Load(Path.Combine(assetsDir, "scene.json"), world);

Console.WriteLine($"Server started. Loaded {world.All().Count()} entities. Fixed tick rate: 60 Hz.");

// SceneLoader already assigned NetworkIds 0..(count-1); dynamically spawned
// player entities continue that sequence.
uint nextNetworkId = (uint)world.All().Count();
int playerSpawnIndex = 0;
const float playerSpeed = 4f;
const float jumpSpeed = 8f;
var peerToEntity = new Dictionary<NetPeer, Entity>();
var lastProcessedSequenceByPeer = new Dictionary<NetPeer, uint>();
var jumpRequestedByPeer = new Dictionary<NetPeer, bool>();

var networkInputSystem = new NetworkInputSystem();
using var physicsWorld = new PhysicsWorld();

using var server = new NetworkServer();

server.ClientConnected += peer =>
{
    Console.WriteLine($"Client connected: {peer.Address}");

    var networkId = new NetworkId(nextNetworkId++);
    var entity = world.CreateEntity();
    world.AddComponent(entity, networkId);
    world.AddComponent(entity, new Transform
    {
        Position = new Vector3D<float>(playerSpawnIndex * 1.5f, 0.5f, -3f),
        Rotation = Quaternion<float>.Identity,
        Scale = Vector3D<float>.One
    });
    world.AddComponent(entity, new RenderInfo { Mesh = "cube.gltf", Texture = "checker.png" });
    world.AddComponent(entity, new Movement { Speed = playerSpeed, Velocity = Vector3D<float>.Zero });
    world.AddComponent(entity, new PlayerControlled());
    world.AddComponent(entity, new Health { Current = 100, Max = 100 });
    world.AddComponent(entity, new Collider { HalfExtents = new Vector3D<float>(0.5f, 0.5f, 0.5f), IsStatic = false });
    playerSpawnIndex++;

    peerToEntity[peer] = entity;

    // New peer gets a full batch (including the player entity we just made for them).
    var fullBatch = world.Query<NetworkId, RenderInfo>()
        .Select(e => (world.GetComponent<NetworkId>(e).Value, world.GetComponent<RenderInfo>(e).Mesh, world.GetComponent<RenderInfo>(e).Texture))
        .ToList();
    var fullBatchWriter = new NetDataWriter();
    EntitySpawnMessage.Write(fullBatchWriter, fullBatch);
    server.Send(peer, fullBatchWriter, DeliveryMethod.ReliableOrdered);

    // Same channel + ReliableOrdered as the spawn batch above, so this is
    // guaranteed to arrive after the entity already exists client-side.
    var yourPlayerWriter = new NetDataWriter();
    YourPlayerMessage.Write(yourPlayerWriter, networkId.Value, playerSpeed);
    server.Send(peer, yourPlayerWriter, DeliveryMethod.ReliableOrdered);

    // Everyone already connected just needs to hear about the new arrival.
    var newPlayerSpawn = new List<(uint, string, string)> { (networkId.Value, "cube.gltf", "checker.png") };
    var newPlayerWriter = new NetDataWriter();
    EntitySpawnMessage.Write(newPlayerWriter, newPlayerSpawn);
    foreach (var otherPeer in server.ConnectedPeers)
    {
        if (otherPeer != peer)
            server.Send(otherPeer, newPlayerWriter, DeliveryMethod.ReliableOrdered);
    }
};

server.ClientDisconnected += peer =>
{
    Console.WriteLine($"Client disconnected: {peer.Address}");

    if (!peerToEntity.Remove(peer, out var entity))
        return;

    uint networkId = world.GetComponent<NetworkId>(entity).Value;
    physicsWorld.RemoveEntity(entity);
    world.DestroyEntity(entity);
    networkInputSystem.DirectionsByNetworkId.Remove(networkId);
    lastProcessedSequenceByPeer.Remove(peer);
    jumpRequestedByPeer.Remove(peer);

    var writer = new NetDataWriter();
    EntityDespawnMessage.Write(writer, networkId);
    server.SendToAll(writer, DeliveryMethod.ReliableOrdered);
};

server.MessageReceived += (peer, reader) =>
{
    if ((MessageType)reader.GetByte() != MessageType.ClientInput)
        return;

    var (sequence, direction, jump) = ClientInputMessage.Read(reader);
    lastProcessedSequenceByPeer[peer] = sequence;
    jumpRequestedByPeer[peer] = jump;
    if (peerToEntity.TryGetValue(peer, out var entity))
        networkInputSystem.DirectionsByNetworkId[world.GetComponent<NetworkId>(entity).Value] = direction;
};

if (args.Contains("--simulate-latency"))
{
    server.SimulateLatency(150, 250);
    Console.WriteLine("Artificial latency simulation ENABLED (150-250ms) - testing only, verifying prediction/reconciliation actually hides it.");
}

server.Start(NetworkConfig.DefaultPort);
Console.WriteLine($"Listening on UDP port {NetworkConfig.DefaultPort}.");

var spinSystem = new SpinSystem();
var accumulator = new FixedTickAccumulator(60.0);

bool running = true;
Console.CancelKeyPress += (_, e) =>
{
    running = false;
    e.Cancel = true;
};

int tickCount = 0;
double reportTimer = 0;
var stopwatch = Stopwatch.StartNew();
double lastElapsedSeconds = 0;

while (running)
{
    double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
    double realDeltaTime = elapsedSeconds - lastElapsedSeconds;
    lastElapsedSeconds = elapsedSeconds;

    server.PollEvents();

    accumulator.Advance(realDeltaTime, fixedDeltaTime =>
    {
        networkInputSystem.Update(world, fixedDeltaTime);
        spinSystem.Update(world, fixedDeltaTime);

        // Physics replaces MovementSystem's naive Position += Velocity * dt
        // for every entity that has a Collider (currently: all of them).
        physicsWorld.SyncNewEntities(world);
        physicsWorld.SyncStaticPoses(world);
        physicsWorld.ApplyVelocities(world);

        // Grounded reflects last tick's post-step raycast - gate here so
        // holding Jump only lets you hop repeatedly, never fly.
        foreach (var (peer, entity) in peerToEntity)
        {
            if (jumpRequestedByPeer.GetValueOrDefault(peer) &&
                world.TryGetComponent<Grounded>(entity, out var grounded) && grounded.Value)
            {
                physicsWorld.ApplyJump(entity, jumpSpeed);
            }
        }

        physicsWorld.Step((float)fixedDeltaTime);
        physicsWorld.SyncTransformsToWorld(world);
        physicsWorld.UpdateGrounded(world);

        tickCount++;

        if (server.ConnectedPeers.Any())
        {
            var snapshot = world.Query<NetworkId, Transform>()
                .Select(entity => (world.GetComponent<NetworkId>(entity).Value, world.GetComponent<Transform>(entity)))
                .ToList();

            // Personalized per peer: same entity list, but each peer gets its
            // own last-processed-input-sequence ack so it can reconcile.
            foreach (var connectedPeer in server.ConnectedPeers)
            {
                uint lastProcessed = lastProcessedSequenceByPeer.GetValueOrDefault(connectedPeer);
                var writer = new NetDataWriter();
                WorldSnapshotMessage.Write(writer, lastProcessed, snapshot);
                server.Send(connectedPeer, writer, DeliveryMethod.Sequenced);
            }
        }
    });

    reportTimer += realDeltaTime;
    if (reportTimer >= 1.0)
    {
        Console.WriteLine($"ticks/sec: {tickCount}");
        tickCount = 0;
        reportTimer = 0;
    }

    Thread.Sleep(1);
}

Console.WriteLine("Server shutting down.");
