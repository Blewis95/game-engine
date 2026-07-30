using System.Diagnostics;
using MyEngine.Core;
using MyEngine.ECS;
using MyEngine.ECS.Systems;
using MyEngine.Scene;

string assetsDir = Path.Combine(AppContext.BaseDirectory, "Assets");
var world = new World();

// No IRenderResourceResolver here: entities get RenderInfo (plain names)
// but never a GPU-backed Render component. This process never touches OpenGL.
SceneLoader.Load(Path.Combine(assetsDir, "scene.json"), world);

Console.WriteLine($"Server started. Loaded {world.All().Count()} entities. Fixed tick rate: 60 Hz.");

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

    accumulator.Advance(realDeltaTime, fixedDeltaTime =>
    {
        spinSystem.Update(world, fixedDeltaTime);
        movementSystem.Update(world, fixedDeltaTime);
        tickCount++;
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
