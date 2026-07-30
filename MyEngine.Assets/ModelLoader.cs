using SharpGLTF.Schema2;

namespace MyEngine.Assets;

/// <summary>
/// Loads the first mesh primitive of a glTF/.glb file into MeshData. A full
/// scene-graph importer (multiple meshes, node hierarchy, materials) is
/// beyond what "asset pipeline basics" needs — that grows alongside the
/// Phase 6 scene format.
/// </summary>
public static class ModelLoader
{
    public static MeshData Load(string path)
    {
        var model = ModelRoot.Load(path);
        var primitive = model.LogicalMeshes[0].Primitives[0];

        var positions = primitive.GetVertexAccessor("POSITION").AsVector3Array();
        var uvs = primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
        var indices = primitive.GetIndexAccessor()!.AsIndicesArray();

        var vertices = new float[positions.Count * 5];
        for (int i = 0; i < positions.Count; i++)
        {
            var position = positions[i];
            var uv = uvs is not null ? uvs[i] : default;

            int offset = i * 5;
            vertices[offset + 0] = position.X;
            vertices[offset + 1] = position.Y;
            vertices[offset + 2] = position.Z;
            vertices[offset + 3] = uv.X;
            vertices[offset + 4] = uv.Y;
        }

        var indexArray = new uint[indices.Count];
        for (int i = 0; i < indices.Count; i++)
            indexArray[i] = indices[i];

        return new MeshData { Vertices = vertices, Indices = indexArray };
    }
}
