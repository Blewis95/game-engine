namespace MyEngine.ECS.Components;

/// <summary>Rotates the entity's Transform around the world Y axis over time.</summary>
public struct Spin
{
    public float RadiansPerSecond;

    public Spin(float radiansPerSecond) => RadiansPerSecond = radiansPerSecond;
}
