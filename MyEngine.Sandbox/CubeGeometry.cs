namespace MyEngine.Sandbox;

/// <summary>
/// Hardcoded unit cube (position + UV per vertex). Standing in for a real
/// loaded mesh until the asset pipeline (Phase 5) can load one from disk.
/// </summary>
internal static class CubeGeometry
{
    public static readonly float[] Vertices =
    {
        // Front (+Z)
        -0.5f, -0.5f,  0.5f,  0f, 0f,
         0.5f, -0.5f,  0.5f,  1f, 0f,
         0.5f,  0.5f,  0.5f,  1f, 1f,
        -0.5f,  0.5f,  0.5f,  0f, 1f,

        // Back (-Z)
         0.5f, -0.5f, -0.5f,  0f, 0f,
        -0.5f, -0.5f, -0.5f,  1f, 0f,
        -0.5f,  0.5f, -0.5f,  1f, 1f,
         0.5f,  0.5f, -0.5f,  0f, 1f,

        // Left (-X)
        -0.5f, -0.5f, -0.5f,  0f, 0f,
        -0.5f, -0.5f,  0.5f,  1f, 0f,
        -0.5f,  0.5f,  0.5f,  1f, 1f,
        -0.5f,  0.5f, -0.5f,  0f, 1f,

        // Right (+X)
         0.5f, -0.5f,  0.5f,  0f, 0f,
         0.5f, -0.5f, -0.5f,  1f, 0f,
         0.5f,  0.5f, -0.5f,  1f, 1f,
         0.5f,  0.5f,  0.5f,  0f, 1f,

        // Top (+Y)
        -0.5f,  0.5f,  0.5f,  0f, 0f,
         0.5f,  0.5f,  0.5f,  1f, 0f,
         0.5f,  0.5f, -0.5f,  1f, 1f,
        -0.5f,  0.5f, -0.5f,  0f, 1f,

        // Bottom (-Y)
        -0.5f, -0.5f, -0.5f,  0f, 0f,
         0.5f, -0.5f, -0.5f,  1f, 0f,
         0.5f, -0.5f,  0.5f,  1f, 1f,
        -0.5f, -0.5f,  0.5f,  0f, 1f,
    };

    public static readonly uint[] Indices = BuildIndices();

    private static uint[] BuildIndices()
    {
        var indices = new uint[6 * 6];
        for (uint face = 0; face < 6; face++)
        {
            uint offset = face * 4;
            uint indexOffset = face * 6;
            indices[indexOffset + 0] = offset + 0;
            indices[indexOffset + 1] = offset + 1;
            indices[indexOffset + 2] = offset + 2;
            indices[indexOffset + 3] = offset + 2;
            indices[indexOffset + 4] = offset + 3;
            indices[indexOffset + 5] = offset + 0;
        }

        return indices;
    }
}
