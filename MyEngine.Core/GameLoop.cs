using Silk.NET.Windowing;

namespace MyEngine.Core;

/// <summary>
/// Drives a Silk.NET window with two independent loops: a fixed-timestep
/// simulation update and a variable-rate render. The render loop only ever
/// reads simulation state (via the interpolation alpha); it never mutates it.
/// </summary>
public sealed class GameLoop
{
    private readonly FixedTickAccumulator _accumulator;

    public IWindow Window { get; }
    public double FixedDeltaTime => _accumulator.FixedDeltaTime;

    public event Action? Load;
    public event Action<double>? FixedUpdate;
    public event Action<double, double>? Render;
    public event Action? Closing;

    public GameLoop(WindowOptions options, double fixedUpdatesPerSecond = 60.0)
    {
        _accumulator = new FixedTickAccumulator(fixedUpdatesPerSecond);

        Window = Silk.NET.Windowing.Window.Create(options);
        Window.Load += () => Load?.Invoke();
        Window.Update += OnUpdate;
        Window.Render += OnRender;
        Window.Closing += () => Closing?.Invoke();
    }

    public void Run() => Window.Run();

    private void OnUpdate(double deltaTime) =>
        _accumulator.Advance(deltaTime, fixedDeltaTime => FixedUpdate?.Invoke(fixedDeltaTime));

    private void OnRender(double deltaTime) =>
        Render?.Invoke(deltaTime, _accumulator.InterpolationAlpha);
}
