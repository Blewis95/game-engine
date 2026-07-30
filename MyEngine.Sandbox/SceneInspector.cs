using System.Numerics;
using ImGuiNET;
using MyEngine.ECS;
using MyEngine.ECS.Components;
using Silk.NET.Maths;

namespace MyEngine.Sandbox;

/// <summary>
/// Bare-bones scene inspector: lists every entity and lets you tweak its
/// Transform live. Intentionally minimal — this is a debug overlay, not the
/// start of a full editor (see the project plan's open decision on that).
/// </summary>
internal static class SceneInspector
{
    public static void Draw(World world)
    {
        ImGui.Begin("Scene Inspector");

        foreach (var entity in world.All())
        {
            if (!ImGui.TreeNode($"Entity {entity.Id}"))
                continue;

            if (world.HasComponent<Transform>(entity))
            {
                ref var transform = ref world.GetComponent<Transform>(entity);

                var position = new Vector3(transform.Position.X, transform.Position.Y, transform.Position.Z);
                if (ImGui.DragFloat3("Position", ref position, 0.05f))
                    transform.Position = new Vector3D<float>(position.X, position.Y, position.Z);

                var scale = new Vector3(transform.Scale.X, transform.Scale.Y, transform.Scale.Z);
                if (ImGui.DragFloat3("Scale", ref scale, 0.02f, 0.01f, 10f))
                    transform.Scale = new Vector3D<float>(scale.X, scale.Y, scale.Z);
            }

            if (world.TryGetComponent<Health>(entity, out var health))
                ImGui.Text($"Health: {health.Current:0}/{health.Max:0}");

            if (world.TryGetComponent<Movement>(entity, out var movement))
                ImGui.Text($"Move speed: {movement.Speed:0.0}");

            if (world.TryGetComponent<Spin>(entity, out var spin))
                ImGui.Text($"Spin: {spin.RadiansPerSecond:0.00} rad/s");

            if (world.HasComponent<PlayerControlled>(entity))
                ImGui.Text("Player controlled");

            ImGui.TreePop();
        }

        ImGui.End();
    }
}
