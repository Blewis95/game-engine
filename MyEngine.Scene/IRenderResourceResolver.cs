namespace MyEngine.Scene;

/// <summary>
/// Resolves render-resource names referenced by a scene file into the
/// opaque handles ECS.Components.Render expects. Scene has no dependency
/// on Rendering, so the app layer supplies the real implementation.
/// </summary>
public interface IRenderResourceResolver
{
    object ResolveMesh(string name);
    object ResolveTexture(string name);
}
