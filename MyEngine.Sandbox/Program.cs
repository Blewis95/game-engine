using MyEngine.Core;
using Silk.NET.Windowing;

var options = WindowOptions.Default with
{
    Title = "MyEngine Sandbox",
    Size = new Silk.NET.Maths.Vector2D<int>(1280, 720)
};

var gameLoop = new GameLoop(options, fixedUpdatesPerSecond: 60.0);

int fixedUpdateCount = 0;
int renderFrameCount = 0;
double reportTimer = 0;

gameLoop.Load += () =>
{
    Console.WriteLine("Window loaded. Fixed tick rate: 60 Hz.");
};

gameLoop.FixedUpdate += _ =>
{
    fixedUpdateCount++;
};

gameLoop.Render += (deltaTime, _) =>
{
    renderFrameCount++;

    reportTimer += deltaTime;
    if (reportTimer >= 1.0)
    {
        Console.WriteLine($"fixed updates/sec: {fixedUpdateCount}, render frames/sec: {renderFrameCount}");
        fixedUpdateCount = 0;
        renderFrameCount = 0;
        reportTimer = 0;
    }
};

gameLoop.Closing += () =>
{
    Console.WriteLine("Window closing.");
};

gameLoop.Run();
