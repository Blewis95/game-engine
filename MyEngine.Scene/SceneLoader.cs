using System.Text.Json;
using MyEngine.ECS;
using MyEngine.ECS.Components;
using Silk.NET.Maths;

namespace MyEngine.Scene;

public static class SceneLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static void Load(string path, World world, IRenderResourceResolver? resourceResolver = null)
    {
        string json = File.ReadAllText(path);
        var document = JsonSerializer.Deserialize<SceneDocument>(json, Options)
            ?? throw new InvalidDataException($"Scene file '{path}' is empty or invalid.");

        uint nextNetworkId = 0;

        foreach (var sceneEntity in document.Entities)
        {
            var entity = world.CreateEntity();
            world.AddComponent(entity, new NetworkId(nextNetworkId++));

            var t = sceneEntity.Transform ?? new SceneTransform();
            world.AddComponent(entity, new Transform
            {
                Position = new Vector3D<float>(t.Position[0], t.Position[1], t.Position[2]),
                Rotation = new Quaternion<float>(t.Rotation[0], t.Rotation[1], t.Rotation[2], t.Rotation[3]),
                Scale = new Vector3D<float>(t.Scale[0], t.Scale[1], t.Scale[2])
            });

            if (sceneEntity.Spin is { } spin)
                world.AddComponent(entity, new Spin(spin.RadiansPerSecond));

            if (sceneEntity.Health is { } health)
                world.AddComponent(entity, new Health { Current = health.Current, Max = health.Max });

            if (sceneEntity.Movement is { } movement)
                world.AddComponent(entity, new Movement { Speed = movement.Speed, Velocity = Vector3D<float>.Zero });

            if (sceneEntity.PlayerControlled)
                world.AddComponent(entity, new PlayerControlled());

            if (sceneEntity.Render is { } render)
            {
                world.AddComponent(entity, new RenderInfo { Mesh = render.Mesh, Texture = render.Texture });

                if (resourceResolver is not null)
                {
                    object mesh = resourceResolver.ResolveMesh(render.Mesh);
                    object texture = resourceResolver.ResolveTexture(render.Texture);
                    world.AddComponent(entity, new Render(mesh, texture));
                }
            }
        }
    }
}
