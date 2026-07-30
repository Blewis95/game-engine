using System.Numerics;
using MyEngine.Assets;
using MyEngine.Core;
using MyEngine.ECS;
using MyEngine.ECS.Components;
using MyEngine.ECS.Systems;
using MyEngine.Rendering;
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
Mesh cubeMesh = null!;
Texture texture = null!;
Camera camera = null!;
InputState input = null!;
Vector2 lastMousePosition = default;

var world = new World();
var spinSystem = new SpinSystem();
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
    var cubeMeshData = ModelLoader.Load(Path.Combine(assetsDir, "cube.gltf"));
    var checkerImage = TextureLoader.Load(Path.Combine(assetsDir, "checker.png"));

    cubeMesh = new Mesh(renderer.Gl, cubeMeshData.Vertices, cubeMeshData.Indices);
    texture = new Texture(renderer.Gl, checkerImage.Pixels, checkerImage.Width, checkerImage.Height);

    // Five cube entities sharing the same mesh/texture, spread along X.
    // Spin rates vary (and the middle one has no Spin component at all)
    // to show the SpinSystem driving simulation state independently per entity.
    float[] spinRates = { -1.5f, -0.75f, 0f, 0.75f, 1.5f };
    for (int i = 0; i < spinRates.Length; i++)
    {
        var entity = world.CreateEntity();

        var transform = Transform.Identity;
        transform.Position = new Vector3D<float>((i - 2) * 2f, 0f, 0f);
        world.AddComponent(entity, transform);

        world.AddComponent(entity, new Render(cubeMesh, texture));

        if (spinRates[i] != 0f)
            world.AddComponent(entity, new Spin(spinRates[i]));
    }

    camera = new Camera
    {
        Position = new Vector3D<float>(0f, 0f, 8f),
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
    if (input.IsKeyDown(Key.Escape))
        gameLoop.Window.Close();

    float distance = moveSpeed * (float)fixedDeltaTime;
    if (input.IsKeyDown(Key.W)) camera.MoveForward(distance);
    if (input.IsKeyDown(Key.S)) camera.MoveForward(-distance);
    if (input.IsKeyDown(Key.D)) camera.MoveRight(distance);
    if (input.IsKeyDown(Key.A)) camera.MoveRight(-distance);
    if (input.IsKeyDown(Key.Space)) camera.MoveUp(distance);
    if (input.IsKeyDown(Key.ControlLeft)) camera.MoveUp(-distance);

    spinSystem.Update(world, fixedDeltaTime);
};

gameLoop.Render += (_, _) =>
{
    renderer.Clear();
    renderSystem.Render(world, shader, camera);
};

gameLoop.Window.FramebufferResize += size =>
{
    renderer.SetViewport(size.X, size.Y);
    camera.AspectRatio = size.X / (float)size.Y;
};

gameLoop.Closing += () =>
{
    cubeMesh.Dispose();
    texture.Dispose();
    shader.Dispose();
    Console.WriteLine("Window closing.");
};

gameLoop.Run();
