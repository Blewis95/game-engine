using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;

namespace MyEngine.Physics;

/// <summary>Records whether a downward ground-check raycast hit anything other than the body casting it.</summary>
internal struct GroundRayHitHandler : IRayHitHandler
{
    public CollidableReference Self;
    public bool Hit;

    public readonly bool AllowTest(CollidableReference collidable) => collidable.Packed != Self.Packed;

    public readonly bool AllowTest(CollidableReference collidable, int childIndex) => true;

    public void OnRayHit(in RayData ray, ref float maximumT, float t, in Vector3 normal, CollidableReference collidable, int childIndex)
    {
        Hit = true;
        maximumT = t;
    }
}
