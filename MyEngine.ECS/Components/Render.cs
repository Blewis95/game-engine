namespace MyEngine.ECS.Components;

/// <summary>
/// Marks an entity as drawable. Holds opaque handles rather than concrete
/// Rendering-module types so ECS stays free of a dependency on Rendering;
/// a render system in the Rendering layer knows how to interpret them.
/// These will likely become real asset handles once the Phase 5 asset
/// pipeline exists.
/// </summary>
public struct Render
{
    public object MeshHandle;
    public object TextureHandle;

    public Render(object meshHandle, object textureHandle)
    {
        MeshHandle = meshHandle;
        TextureHandle = textureHandle;
    }
}
