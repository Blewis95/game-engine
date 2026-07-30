using MyEngine.Assets;
using MyEngine.Rendering;
using MyEngine.Scene;

namespace MyEngine.Sandbox;

/// <summary>Loads and caches meshes/textures referenced by name from a scene file, bridging Scene's decoupled asset names to real GPU resources.</summary>
internal sealed class RenderResourceResolver : IRenderResourceResolver
{
    private readonly Silk.NET.OpenGL.GL _gl;
    private readonly string _assetsDir;
    private readonly Dictionary<string, Mesh> _meshes = new();
    private readonly Dictionary<string, Texture> _textures = new();

    public RenderResourceResolver(Silk.NET.OpenGL.GL gl, string assetsDir)
    {
        _gl = gl;
        _assetsDir = assetsDir;
    }

    public object ResolveMesh(string name)
    {
        if (!_meshes.TryGetValue(name, out var mesh))
        {
            var data = ModelLoader.Load(Path.Combine(_assetsDir, name));
            mesh = new Mesh(_gl, data.Vertices, data.Indices);
            _meshes[name] = mesh;
        }

        return mesh;
    }

    public object ResolveTexture(string name)
    {
        if (!_textures.TryGetValue(name, out var texture))
        {
            var data = TextureLoader.Load(Path.Combine(_assetsDir, name));
            texture = new Texture(_gl, data.Pixels, data.Width, data.Height);
            _textures[name] = texture;
        }

        return texture;
    }

    public void DisposeAll()
    {
        foreach (var mesh in _meshes.Values)
            mesh.Dispose();
        foreach (var texture in _textures.Values)
            texture.Dispose();
    }
}
