using System.Numerics;
using ImGuiNET;
using MyEngine.Core;
using MyEngine.ECS;
using MyEngine.ECS.Systems;
using MyEngine.Rendering;
using MyEngine.Sandbox;
using MyEngine.Scene;
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
SpinSystem spinSystem = null!;
MovementSystem movementSystem = null!;
InputMovementSystem inputMovementSystem = null!;
Vector2 lastMousePosition = default;
bool cameraLookActive = false;

var world = new World();
var renderSystem = new RenderSystem();

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
    SceneLoader.Load(Path.Combine(assetsDir, "scene.json"), world, resourceResolver);

    camera = new Camera
    {
        Position = new Vector3D<float>(0f, 6f, 12f),
        AspectRatio = gameLoop.Window.FramebufferSize.X / (float)gameLoop.Window.FramebufferSize.Y
    };
    camera.Look(0f, -25f);

    input = new InputState(gameLoop.Window);
    imGuiController = new ImGuiController(renderer.Gl, gameLoop.Window, input.Context);
    spinSystem = new SpinSystem();
    movementSystem = new MovementSystem();
    inputMovementSystem = new InputMovementSystem(input);

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
    Console.WriteLine("Hold right mouse button + WASD/space/ctrl: fly camera. Arrow keys: move the player cube. Esc: quit.");
};

gameLoop.FixedUpdate += fixedDeltaTime =>
{
    if (input.IsKeyDown(Key.Escape))
        gameLoop.Window.Close();

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

    inputMovementSystem.Update(world, fixedDeltaTime);
    movementSystem.Update(world, fixedDeltaTime);
    spinSystem.Update(world, fixedDeltaTime);
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
    imGuiController.Dispose();
    resourceResolver.DisposeAll();
    shader.Dispose();
    Console.WriteLine("Window closing.");
};

gameLoop.Run();
