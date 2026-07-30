using StbImageSharp;

namespace MyEngine.Assets;

public static class TextureLoader
{
    public static ImageData Load(string path)
    {
        using var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        return new ImageData
        {
            Pixels = image.Data,
            Width = (uint)image.Width,
            Height = (uint)image.Height
        };
    }
}
