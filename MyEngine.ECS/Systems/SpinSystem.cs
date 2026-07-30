using MyEngine.ECS.Components;
using Silk.NET.Maths;

namespace MyEngine.ECS.Systems;

public sealed class SpinSystem : ISystem
{
    public void Update(World world, double fixedDeltaTime)
    {
        foreach (var entity in world.Query<Transform, Spin>())
        {
            var spin = world.GetComponent<Spin>(entity);
            float angle = spin.RadiansPerSecond * (float)fixedDeltaTime;

            ref var transform = ref world.GetComponent<Transform>(entity);
            var delta = Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, angle);
            transform.Rotation = Quaternion<float>.Normalize(delta * transform.Rotation);
        }
    }
}
