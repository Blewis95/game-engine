using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace MyEngine.Rendering;

public sealed class Renderer
{
    public GL Gl { get; }

    public Renderer(IWindow window)
    {
        Gl = window.CreateOpenGL();
        Gl.Enable(EnableCap.DepthTest);
    }

    public void Clear(float r = 0.1f, float g = 0.1f, float b = 0.15f, float a = 1f)
    {
        Gl.ClearColor(r, g, b, a);
        Gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
    }

    public void SetViewport(int width, int height) => Gl.Viewport(0, 0, (uint)width, (uint)height);
}
