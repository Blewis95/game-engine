using MyEngine.ECS.Components;
using Silk.NET.Maths;

namespace MyEngine.ECS.Systems;

/// <summary>
/// Applies externally-supplied movement intent to Velocity on any entity
/// that is both Movement and PlayerControlled. Mirrors InputMovementSystem,
/// but the intent is set by the caller each tick (e.g. from a decoded
/// ClientInputMessage) instead of read from local input — this is what lets
/// a headless server drive player movement without ever touching Silk.NET.Input.
/// </summary>
public sealed class NetworkInputSystem : ISystem
{
    public Vector3D<float> MoveDirection { get; set; }

    public void Update(World world, double fixedDeltaTime)
    {
        foreach (var entity in world.Query<Movement, PlayerControlled>())
        {
            ref var movement = ref world.GetComponent<Movement>(entity);
            movement.Velocity = MoveDirection * movement.Speed;
        }
    }
}
