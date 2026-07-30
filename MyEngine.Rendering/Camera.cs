using Silk.NET.Maths;

namespace MyEngine.Rendering;

/// <summary>
/// Basic fly camera (position + yaw/pitch). This is a dev/viewport tool, not
/// simulation state — it will likely attach to an ECS Transform once the ECS
/// lands in Phase 4.
/// </summary>
public sealed class Camera
{
    private const float MinPitch = -89f;
    private const float MaxPitch = 89f;

    private float _yaw = -90f;
    private float _pitch;
    private Vector3D<float> _front = -Vector3D<float>.UnitZ;
    private Vector3D<float> _right = Vector3D<float>.UnitX;
    private readonly Vector3D<float> _worldUp = Vector3D<float>.UnitY;

    public Vector3D<float> Position { get; set; } = new(0, 0, 3);
    public float FovDegrees { get; set; } = 60f;
    public float AspectRatio { get; set; } = 16f / 9f;
    public float NearPlane { get; set; } = 0.1f;
    public float FarPlane { get; set; } = 100f;

    public void Look(float yawDelta, float pitchDelta)
    {
        _yaw += yawDelta;
        _pitch = Math.Clamp(_pitch + pitchDelta, MinPitch, MaxPitch);
        UpdateVectors();
    }

    public void MoveForward(float distance) => Position += _front * distance;
    public void MoveRight(float distance) => Position += _right * distance;
    public void MoveUp(float distance) => Position += _worldUp * distance;

    public Matrix4X4<float> GetViewMatrix() => Matrix4X4.CreateLookAt(Position, Position + _front, _worldUp);

    public Matrix4X4<float> GetProjectionMatrix() =>
        Matrix4X4.CreatePerspectiveFieldOfView(DegreesToRadians(FovDegrees), AspectRatio, NearPlane, FarPlane);

    private void UpdateVectors()
    {
        float yawRad = DegreesToRadians(_yaw);
        float pitchRad = DegreesToRadians(_pitch);

        _front = Vector3D.Normalize(new Vector3D<float>(
            MathF.Cos(yawRad) * MathF.Cos(pitchRad),
            MathF.Sin(pitchRad),
            MathF.Sin(yawRad) * MathF.Cos(pitchRad)));

        _right = Vector3D.Normalize(Vector3D.Cross(_front, _worldUp));
    }

    private static float DegreesToRadians(float degrees) => degrees * MathF.PI / 180f;
}
