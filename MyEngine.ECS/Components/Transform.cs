using Silk.NET.Maths;

namespace MyEngine.ECS.Components;

public struct Transform
{
    public Vector3D<float> Position;
    public Quaternion<float> Rotation;
    public Vector3D<float> Scale;

    public static Transform Identity => new()
    {
        Position = Vector3D<float>.Zero,
        Rotation = Quaternion<float>.Identity,
        Scale = Vector3D<float>.One
    };

    public readonly Matrix4X4<float> GetMatrix() =>
        Matrix4X4.CreateScale(Scale) * Matrix4X4.CreateFromQuaternion(Rotation) * Matrix4X4.CreateTranslation(Position);
}
