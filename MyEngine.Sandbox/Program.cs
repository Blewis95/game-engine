using System.Numerics;
using ImGuiNET;
using LiteNetLib;
using LiteNetLib.Utils;
using MyEngine.Core;
using MyEngine.ECS;
using MyEngine.ECS.Components;
using MyEngine.Networking;
using MyEngine.Rendering;
using MyEngine.Sandbox;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

var options = WindowOptions.Default with
{
    Title = "MyEngine Sandbox",
    Size = new Vector2D<int>(1280, 720)
};

var gameLoop = new GameLoop(options, fixedUpdatesPerSecond: 60.0);

Renderer renderer = null!;
Shader shader = null!;
RenderResourceResolver resourceResolver = null!;
Camera camera = null!;
InputState input = null!;
ImGuiController imGuiController = null!;
NetworkClient networkClient = null!;
Vector2 lastMousePosition = default;
bool cameraLookActive = false;

var world = new World();
var renderSystem = new RenderSystem();

// Server owns simulation; this maps its NetworkId to our local (render-only) Entity.
var networkIdToEntity = new Dictionary<uint, Entity>();

const float moveSpeed = 3f;
const float mouseSensitivity = 0.1f;

gameLoop.Load += () =>
{
    renderer = new Renderer(gameLoop.Window);
    renderer.SetViewport(gameLoop.Window.FramebufferSize.X, gameLoop.Window.FramebufferSize.Y);

    string vertexSource = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Shaders", "basic.vert"));
    string fragmentSource = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Shaders", "basic.frag"));
    shader = new Shader(renderer.Gl, vertexSource, fragmentSource);

    string assetsDir = Path.Combine(AppContext.BaseDirectory, "Assets");
    resourceResolver = new RenderResourceResolver(renderer.Gl, assetsDir);

    camera = new Camera
    {
        Position = new Vector3D<float>(0f, 6f, 12f),
        AspectRatio = gameLoop.Window.FramebufferSize.X / (float)gameLoop.Window.FramebufferSize.Y
    };
    camera.Look(0f, -25f);

    input = new InputState(gameLoop.Window);
    imGuiController = new ImGuiController(renderer.Gl, gameLoop.Window, input.Context);

    networkClient = new NetworkClient();
    networkClient.Connected += peer => Console.WriteLine($"Connected to server ({peer.Address}).");
    networkClient.Disconnected += () => Console.WriteLine("Disconnected from server.");
    networkClient.MessageReceived += reader =>
    {
        var messageType = (MessageType)reader.GetByte();
        switch (messageType)
        {
            case MessageType.EntitySpawn:
                foreach (var (networkId, mesh, texture) in EntitySpawnMessage.Read(reader))
                {
                    if (networkIdToEntity.ContainsKey(networkId))
                        continue;

                    var entity = world.CreateEntity();
                    world.AddComponent(entity, Transform.Identity);
                    world.AddComponent(entity, new Render(resourceResolver.ResolveMesh(mesh), resourceResolver.ResolveTexture(texture)));
                    networkIdToEntity[networkId] = entity;
                }
                break;

            case MessageType.WorldSnapshot:
                // LastProcessedInputSequence isn't consumed yet - that's Phase 3 (reconciliation).
                var (_, snapshotEntities) = WorldSnapshotMessage.Read(reader);
                foreach (var (networkId, transform) in snapshotEntities)
                {
                    if (networkIdToEntity.TryGetValue(networkId, out var entity))
                        world.GetComponent<Transform>(entity) = transform;
                }
                break;

            case MessageType.EntityDespawn:
                uint despawnedId = EntityDespawnMessage.Read(reader);
                if (networkIdToEntity.Remove(despawnedId, out var despawnedEntity))
                    world.DestroyEntity(despawnedEntity);
                break;
        }
    };
    networkClient.Connect("127.0.0.1");
    Console.WriteLine($"Connecting to server at 127.0.0.1:{NetworkConfig.DefaultPort}...");

    // Hold right mouse button to fly the camera (raw/hidden cursor); release
    // it to get a normal clickable cursor back for the scene inspector.
    var mouse = input.PrimaryMouse;
    if (mouse is not null)
    {
        mouse.MouseDown += (_, button) =>
        {
            if (button != MouseButton.Right) return;
            cameraLookActive = true;
            mouse.Cursor.CursorMode = CursorMode.Raw;
            lastMousePosition = mouse.Position;
        };

        mouse.MouseUp += (_, button) =>
        {
            if (button != MouseButton.Right) return;
            cameraLookActive = false;
            mouse.Cursor.CursorMode = CursorMode.Normal;
        };

        mouse.MouseMove += (_, position) =>
        {
            if (!cameraLookActive || ImGui.GetIO().WantCaptureMouse)
                return;

            var delta = position - lastMousePosition;
            lastMousePosition = position;
            camera.Look(delta.X * mouseSensitivity, -delta.Y * mouseSensitivity);
        };
    }

    Console.WriteLine("Window loaded. Fixed tick rate: 60 Hz.");
    Console.WriteLine("Hold right mouse button + WASD/space/ctrl: fly camera. Arrow keys: move the player cube (server-authoritative - expect a little lag). Esc: quit.");
    Console.WriteLine("The scene is now entirely server-driven - this window just renders whatever it's sent.");
};

gameLoop.FixedUpdate += fixedDeltaTime =>
{
    if (input.IsKeyDown(Key.Escape))
        gameLoop.Window.Close();

    networkClient.PollEvents();

    var moveDirection = Vector3D<float>.Zero;
    if (input.IsKeyDown(Key.Up)) moveDirection -= Vector3D<float>.UnitZ;
    if (input.IsKeyDown(Key.Down)) moveDirection += Vector3D<float>.UnitZ;
    if (input.IsKeyDown(Key.Right)) moveDirection += Vector3D<float>.UnitX;
    if (input.IsKeyDown(Key.Left)) moveDirection -= Vector3D<float>.UnitX;
    if (moveDirection.LengthSquared > 0f)
        moveDirection = Vector3D.Normalize(moveDirection);

    // Sequence is a placeholder (always 0) until Phase 3 wires up PredictedMovement.
    var inputWriter = new NetDataWriter();
    ClientInputMessage.Write(inputWriter, 0, moveDirection);
    networkClient.Send(inputWriter, DeliveryMethod.Sequenced);

    if (cameraLookActive)
    {
        float distance = moveSpeed * (float)fixedDeltaTime;
        if (input.IsKeyDown(Key.W)) camera.MoveForward(distance);
        if (input.IsKeyDown(Key.S)) camera.MoveForward(-distance);
        if (input.IsKeyDown(Key.D)) camera.MoveRight(distance);
        if (input.IsKeyDown(Key.A)) camera.MoveRight(-distance);
        if (input.IsKeyDown(Key.Space)) camera.MoveUp(distance);
        if (input.IsKeyDown(Key.ControlLeft)) camera.MoveUp(-distance);
    }
};

gameLoop.Render += (deltaTime, _) =>
{
    renderer.Clear();
    renderSystem.Render(world, shader, camera);

    imGuiController.Update((float)deltaTime);
    SceneInspector.Draw(world);
    imGuiController.Render();
};

gameLoop.Window.FramebufferResize += size =>
{
    renderer.SetViewport(size.X, size.Y);
    camera.AspectRatio = size.X / (float)size.Y;
};

gameLoop.Closing += () =>
{
    networkClient.Dispose();
    imGuiController.Dispose();
    resourceResolver.DisposeAll();
    shader.Dispose();
    Console.WriteLine("Window closing.");
};

gameLoop.Run();
