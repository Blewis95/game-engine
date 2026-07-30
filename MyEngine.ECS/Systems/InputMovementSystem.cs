using MyEngine.Core;
using MyEngine.ECS.Components;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace MyEngine.ECS.Systems;

/// <summary>
/// Basic input-to-movement binding: drives Velocity on any entity that is
/// both Movement and PlayerControlled. Deliberately minimal (world-axis
/// aligned, no acceleration/camera-relative steering) — real player control
/// belongs to the game layer, not the engine.
/// </summary>
public sealed class InputMovementSystem : ISystem
{
    private readonly InputState _input;

    public InputMovementSystem(InputState input) => _input = input;

    public void Update(World world, double fixedDeltaTime)
    {
        var direction = Vector3D<float>.Zero;
        if (_input.IsKeyDown(Key.Up)) direction -= Vector3D<float>.UnitZ;
        if (_input.IsKeyDown(Key.Down)) direction += Vector3D<float>.UnitZ;
        if (_input.IsKeyDown(Key.Right)) direction += Vector3D<float>.UnitX;
        if (_input.IsKeyDown(Key.Left)) direction -= Vector3D<float>.UnitX;

        if (direction.LengthSquared > 0f)
            direction = Vector3D.Normalize(direction);

        foreach (var entity in world.Query<Movement, PlayerControlled>())
        {
            ref var movement = ref world.GetComponent<Movement>(entity);
            movement.Velocity = direction * movement.Speed;
        }
    }
}
