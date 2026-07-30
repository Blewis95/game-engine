using MyEngine.ECS.Components;

namespace MyEngine.ECS.Systems;

/// <summary>Integrates Movement.Velocity into Transform.Position. Fully generic — doesn't care what set the velocity.</summary>
public sealed class MovementSystem : ISystem
{
    public void Update(World world, double fixedDeltaTime)
    {
        foreach (var entity in world.Query<Transform, Movement>())
        {
            var movement = world.GetComponent<Movement>(entity);
            ref var transform = ref world.GetComponent<Transform>(entity);
            transform.Position += movement.Velocity * (float)fixedDeltaTime;
        }
    }
}
