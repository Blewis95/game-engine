using System.Diagnostics;
using LiteNetLib;
using LiteNetLib.Utils;
using MyEngine.Core;
using MyEngine.ECS;
using MyEngine.ECS.Components;
using MyEngine.ECS.Systems;
using MyEngine.Networking;
using MyEngine.Scene;

string assetsDir = Path.Combine(AppContext.BaseDirectory, "Assets");
var world = new World();

// No IRenderResourceResolver here: entities get RenderInfo (plain names)
// but never a GPU-backed Render component. This process never touches OpenGL.
SceneLoader.Load(Path.Combine(assetsDir, "scene.json"), world);

Console.WriteLine($"Server started. Loaded {world.All().Count()} entities. Fixed tick rate: 60 Hz.");

using var server = new NetworkServer();
server.ClientConnected += peer =>
{
    Console.WriteLine($"Client connected: {peer.Address}");

    var spawns = world.Query<NetworkId, RenderInfo>()
        .Select(entity =>
        {
            var networkId = world.GetComponent<NetworkId>(entity);
            var renderInfo = world.GetComponent<RenderInfo>(entity);
            return (networkId.Value, renderInfo.Mesh, renderInfo.Texture);
        })
        .ToList();

    var writer = new NetDataWriter();
    EntitySpawnMessage.Write(writer, spawns);
    server.Send(peer, writer, DeliveryMethod.ReliableOrdered);
};
server.ClientDisconnected += peer => Console.WriteLine($"Client disconnected: {peer.Address}");

var networkInputSystem = new NetworkInputSystem();
server.MessageReceived += (_, reader) =>
{
    if ((MessageType)reader.GetByte() == MessageType.ClientInput)
        networkInputSystem.MoveDirection = ClientInputMessage.Read(reader);
};

server.Start(NetworkConfig.DefaultPort);
Console.WriteLine($"Listening on UDP port {NetworkConfig.DefaultPort}.");

var spinSystem = new SpinSystem();
var movementSystem = new MovementSystem();
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
        movementSystem.Update(world, fixedDeltaTime);
        spinSystem.Update(world, fixedDeltaTime);
        tickCount++;

        if (server.ConnectedPeers.Any())
        {
            var snapshot = world.Query<NetworkId, Transform>()
                .Select(entity => (world.GetComponent<NetworkId>(entity).Value, world.GetComponent<Transform>(entity)))
                .ToList();

            var writer = new NetDataWriter();
            WorldSnapshotMessage.Write(writer, snapshot);
            server.SendToAll(writer, DeliveryMethod.Sequenced);
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
