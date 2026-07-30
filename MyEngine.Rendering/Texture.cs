using Silk.NET.OpenGL;

namespace MyEngine.Rendering;

public sealed unsafe class Texture : IDisposable
{
    private readonly GL _gl;
    private readonly uint _handle;

    public Texture(GL gl, byte[] rgbaPixels, uint width, uint height)
    {
        _gl = gl;
        _handle = _gl.GenTexture();
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _handle);

        fixed (byte* data = rgbaPixels)
        {
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
        }

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        _gl.GenerateMipmap(TextureTarget.Texture2D);
    }

    /// <summary>
    /// Builds a placeholder checkerboard texture without any file/asset dependency.
    /// Useful until the real asset pipeline (Phase 5) can load textures from disk.
    /// </summary>
    public static Texture CreateCheckerboard(GL gl, uint size = 256, uint squares = 8)
    {
        var pixels = new byte[size * size * 4];
        uint squareSize = size / squares;

        for (uint y = 0; y < size; y++)
        {
            for (uint x = 0; x < size; x++)
            {
                bool isLight = ((x / squareSize) + (y / squareSize)) % 2 == 0;
                byte value = isLight ? (byte)230 : (byte)60;

                uint index = (y * size + x) * 4;
                pixels[index + 0] = value;
                pixels[index + 1] = value;
                pixels[index + 2] = value;
                pixels[index + 3] = 255;
            }
        }

        return new Texture(gl, pixels, size, size);
    }

    public void Bind(TextureUnit unit = TextureUnit.Texture0)
    {
        _gl.ActiveTexture(unit);
        _gl.BindTexture(TextureTarget.Texture2D, _handle);
    }

    public void Dispose() => _gl.DeleteTexture(_handle);
}
