namespace MyEngine.Scene;

public sealed class SceneDocument
{
    public List<SceneEntity> Entities { get; init; } = new();
}

public sealed class SceneEntity
{
    public SceneTransform? Transform { get; init; }
    public SceneRender? Render { get; init; }
    public SceneSpin? Spin { get; init; }
    public SceneHealth? Health { get; init; }
    public SceneMovement? Movement { get; init; }
    public bool PlayerControlled { get; init; }
}

public sealed class SceneTransform
{
    public float[] Position { get; init; } = { 0f, 0f, 0f };
    public float[] Rotation { get; init; } = { 0f, 0f, 0f, 1f };
    public float[] Scale { get; init; } = { 1f, 1f, 1f };
}

public sealed class SceneRender
{
    public required string Mesh { get; init; }
    public required string Texture { get; init; }
}

public sealed class SceneSpin
{
    public float RadiansPerSecond { get; init; }
}

public sealed class SceneHealth
{
    public float Current { get; init; }
    public float Max { get; init; }
}

public sealed class SceneMovement
{
    public float Speed { get; init; }
}
