namespace MyEngine.Core;

/// <summary>
/// The "fix your timestep" accumulator pattern, decoupled from windowing so
/// both a windowed loop (GameLoop) and a headless loop (e.g. a dedicated
/// server) can drive simulation at an identical, frame-rate-independent
/// fixed tick rate.
/// </summary>
public sealed class FixedTickAccumulator
{
    private double _accumulator;

    public double FixedDeltaTime { get; }

    /// <summary>Fraction of the way into the next fixed tick — for render interpolation.</summary>
    public double InterpolationAlpha => _accumulator / FixedDeltaTime;

    public FixedTickAccumulator(double fixedUpdatesPerSecond)
    {
        FixedDeltaTime = 1.0 / fixedUpdatesPerSecond;
    }

    /// <summary>Call once per real frame/loop iteration. Invokes onFixedUpdate zero or more times to catch up.</summary>
    public void Advance(double realDeltaTime, Action<double> onFixedUpdate)
    {
        _accumulator += realDeltaTime;

        while (_accumulator >= FixedDeltaTime)
        {
            onFixedUpdate(FixedDeltaTime);
            _accumulator -= FixedDeltaTime;
        }
    }
}
