using Silk.NET.Maths;

namespace MyEngine.ECS.Components;

/// <summary>
/// Current motion state for an entity. Velocity is plain data so anything
/// can drive it — local input, AI, or (eventually) network state — while
/// MovementSystem is the single place that integrates it into Transform.
/// </summary>
public struct Movement
{
    public float Speed;
    public Vector3D<float> Velocity;
}
