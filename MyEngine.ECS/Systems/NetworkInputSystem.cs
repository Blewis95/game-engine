using MyEngine.ECS.Components;
using Silk.NET.Maths;

namespace MyEngine.ECS.Systems;

/// <summary>
/// Applies externally-supplied movement intent to Velocity on every
/// NetworkId + Movement + PlayerControlled entity, looked up per entity by
/// its NetworkId. Mirrors InputMovementSystem, but the intent comes from
/// the caller (e.g. one direction per connected client, decoded from
/// ClientInputMessage) instead of local input, and multiple entities can be
/// driven independently — one per player.
/// </summary>
public sealed class NetworkInputSystem : ISystem
{
    public Dictionary<uint, Vector3D<float>> DirectionsByNetworkId { get; } = new();

    public void Update(World world, double fixedDeltaTime)
    {
        foreach (var entity in world.Query<NetworkId, Movement, PlayerControlled>())
        {
            uint networkId = world.GetComponent<NetworkId>(entity).Value;
            DirectionsByNetworkId.TryGetValue(networkId, out var direction);

            ref var movement = ref world.GetComponent<Movement>(entity);
            movement.Velocity = direction * movement.Speed;
        }
    }
}
