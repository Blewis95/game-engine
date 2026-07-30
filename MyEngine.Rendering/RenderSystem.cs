using MyEngine.ECS;
using MyEngine.ECS.Components;

namespace MyEngine.Rendering;

/// <summary>
/// Reads Transform + Render component data and draws it — the render-loop
/// half of the sim/render split. Never mutates World state.
/// </summary>
public sealed class RenderSystem
{
    public void Render(World world, Shader shader, Camera camera)
    {
        shader.Use();
        shader.SetUniform("uView", camera.GetViewMatrix());
        shader.SetUniform("uProjection", camera.GetProjectionMatrix());

        foreach (var entity in world.Query<Transform, Render>())
        {
            var transform = world.GetComponent<Transform>(entity);
            var render = world.GetComponent<Render>(entity);

            shader.SetUniform("uModel", transform.GetMatrix());

            ((Texture)render.TextureHandle).Bind();
            shader.SetUniform("uTexture", 0);

            ((Mesh)render.MeshHandle).Draw();
        }
    }
}
