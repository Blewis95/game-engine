namespace MyEngine.ECS.Components;

/// <summary>
/// Which mesh/texture (by name) an entity should render as. Plain data —
/// unlike Render, this holds no GPU handles, so it's safe on a process with
/// no OpenGL context (e.g. a headless server). A client resolves this into
/// an actual Render component once it has a GL context.
/// </summary>
public struct RenderInfo
{
    public string Mesh;
    public string Texture;
}
