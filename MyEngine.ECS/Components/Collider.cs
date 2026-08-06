using Silk.NET.Maths;

namespace MyEngine.ECS.Components;

/// <summary>
/// Box collision shape descriptor. Plain data - no physics-backend handle
/// types, mirroring the RenderInfo/Render split so ECS stays decoupled from
/// whatever physics library is behind it. Safe to hold anywhere, including
/// the client, which never acts on it.
/// </summary>
public struct Collider
{
    public Vector3D<float> HalfExtents;
    public bool IsStatic;
}
