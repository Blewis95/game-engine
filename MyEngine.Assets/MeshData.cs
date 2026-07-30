namespace MyEngine.Assets;

/// <summary>
/// Plain-data mesh: interleaved position(3) + UV(2) per vertex, matching
/// MyEngine.Rendering.Mesh's vertex layout. Assets stays decoupled from
/// Rendering/OpenGL — turning this into a GPU resource is the caller's job.
/// </summary>
public sealed class MeshData
{
    public required float[] Vertices { get; init; }
    public required uint[] Indices { get; init; }
}
