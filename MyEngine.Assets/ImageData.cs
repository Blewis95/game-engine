namespace MyEngine.Assets;

/// <summary>Decoded image as raw RGBA8 pixels, matching MyEngine.Rendering.Texture's expected format.</summary>
public sealed class ImageData
{
    public required byte[] Pixels { get; init; }
    public required uint Width { get; init; }
    public required uint Height { get; init; }
}
