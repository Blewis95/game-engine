using LiteNetLib.Utils;
using MyEngine.ECS.Components;
using Silk.NET.Maths;

namespace MyEngine.Networking;

/// <summary>Per-tick authoritative Transform for every replicated entity, keyed by NetworkId.</summary>
public static class WorldSnapshotMessage
{
    public static void Write(NetDataWriter writer, IReadOnlyList<(uint NetworkId, Transform Transform)> entities)
    {
        writer.Put((byte)MessageType.WorldSnapshot);
        writer.Put((ushort)entities.Count);

        foreach (var (networkId, transform) in entities)
        {
            writer.Put(networkId);
            WriteTransform(writer, transform);
        }
    }

    public static List<(uint NetworkId, Transform Transform)> Read(NetDataReader reader)
    {
        ushort count = reader.GetUShort();
        var result = new List<(uint, Transform)>(count);

        for (int i = 0; i < count; i++)
        {
            uint networkId = reader.GetUInt();
            var transform = ReadTransform(reader);
            result.Add((networkId, transform));
        }

        return result;
    }

    private static void WriteTransform(NetDataWriter writer, Transform t)
    {
        writer.Put(t.Position.X);
        writer.Put(t.Position.Y);
        writer.Put(t.Position.Z);
        writer.Put(t.Rotation.X);
        writer.Put(t.Rotation.Y);
        writer.Put(t.Rotation.Z);
        writer.Put(t.Rotation.W);
        writer.Put(t.Scale.X);
        writer.Put(t.Scale.Y);
        writer.Put(t.Scale.Z);
    }

    private static Transform ReadTransform(NetDataReader reader)
    {
        return new Transform
        {
            Position = new Vector3D<float>(reader.GetFloat(), reader.GetFloat(), reader.GetFloat()),
            Rotation = new Quaternion<float>(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat()),
            Scale = new Vector3D<float>(reader.GetFloat(), reader.GetFloat(), reader.GetFloat())
        };
    }
}
