namespace MyEngine.ECS.Components;

/// <summary>
/// Generic health stub. No damage/death system consumes this yet — combat is
/// explicitly out of scope for the engine (that belongs to the game layer).
/// </summary>
public struct Health
{
    public float Current;
    public float Max;

    public readonly bool IsDead => Current <= 0f;
}
