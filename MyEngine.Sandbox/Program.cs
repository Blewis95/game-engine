using System.Numerics;
using MyEngine.Core;
using MyEngine.Rendering;
using MyEngine.Sandbox;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

var options = WindowOptions.Default with
{
    Title = "MyEngine Sandbox",
    Size = new Vector2D<int>(1280, 720)
};

var gameLoop = new GameLoop(options, fixedUpdatesPerSecond: 60.0);

Renderer renderer = null!;
Shader shader = null!;
Mesh cube = null!;
Texture texture = null!;
Camera camera = null!;
InputState input = null!;
Vector2 lastMousePosition = default;
double simulationTime = 0;

const float moveSpeed = 3f;
const float mouseSensitivity = 0.1f;

gameLoop.Load += () =>
{
    renderer = new Renderer(gameLoop.Window);
    renderer.SetViewport(gameLoop.Window.FramebufferSize.X, gameLoop.Window.FramebufferSize.Y);

    string vertexSource = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Shaders", "basic.vert"));
    string fragmentSource = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Shaders", "basic.frag"));
    shader = new Shader(renderer.Gl, vertexSource, fragmentSource);

    cube = new Mesh(renderer.Gl, CubeGeometry.Vertices, CubeGeometry.Indices);
    texture = Texture.CreateCheckerboard(renderer.Gl);

    camera = new Camera
    {
        AspectRatio = gameLoop.Window.FramebufferSize.X / (float)gameLoop.Window.FramebufferSize.Y
    };

    input = new InputState(gameLoop.Window);
    var mouse = input.PrimaryMouse;
    if (mouse is not null)
    {
        mouse.Cursor.CursorMode = CursorMode.Raw;
        lastMousePosition = mouse.Position;
        mouse.MouseMove += (_, position) =>
        {
            var delta = position - lastMousePosition;
            lastMousePosition = position;
            camera.Look(delta.X * mouseSensitivity, -delta.Y * mouseSensitivity);
        };
    }

    Console.WriteLine("Window loaded. Fixed tick rate: 60 Hz. WASD to move, mouse to look, Esc to quit.");
};

gameLoop.FixedUpdate += fixedDeltaTime =>
{
    simulationTime += fixedDeltaTime;

    if (input.IsKeyDown(Key.Escape))
        gameLoop.Window.Close();

    float distance = moveSpeed * (float)fixedDeltaTime;
    if (input.IsKeyDown(Key.W)) camera.MoveForward(distance);
    if (input.IsKeyDown(Key.S)) camera.MoveForward(-distance);
    if (input.IsKeyDown(Key.D)) camera.MoveRight(distance);
    if (input.IsKeyDown(Key.A)) camera.MoveRight(-distance);
    if (input.IsKeyDown(Key.Space)) camera.MoveUp(distance);
    if (input.IsKeyDown(Key.ControlLeft)) camera.MoveUp(-distance);
};

gameLoop.Render += (_, _) =>
{
    renderer.Clear();

    shader.Use();
    shader.SetUniform("uView", camera.GetViewMatrix());
    shader.SetUniform("uProjection", camera.GetProjectionMatrix());

    var model = Matrix4X4.CreateRotationY((float)simulationTime);
    shader.SetUniform("uModel", model);

    texture.Bind();
    shader.SetUniform("uTexture", 0);

    cube.Draw();
};

gameLoop.Window.FramebufferResize += size =>
{
    renderer.SetViewport(size.X, size.Y);
    camera.AspectRatio = size.X / (float)size.Y;
};

gameLoop.Closing += () =>
{
    cube.Dispose();
    texture.Dispose();
    shader.Dispose();
    Console.WriteLine("Window closing.");
};

gameLoop.Run();
