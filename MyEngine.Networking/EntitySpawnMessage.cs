using LiteNetLib.Utils;

namespace MyEngine.Networking;

/// <summary>Tells a client which replicated entities exist and what to render them as (mesh/texture names, not GPU handles).</summary>
public static class EntitySpawnMessage
{
    public static void Write(NetDataWriter writer, IReadOnlyList<(uint NetworkId, string Mesh, string Texture)> entities)
    {
        writer.Put((byte)MessageType.EntitySpawn);
        writer.Put((ushort)entities.Count);

        foreach (var (networkId, mesh, texture) in entities)
        {
            writer.Put(networkId);
            writer.Put(mesh);
            writer.Put(texture);
        }
    }

    public static List<(uint NetworkId, string Mesh, string Texture)> Read(NetDataReader reader)
    {
        ushort count = reader.GetUShort();
        var result = new List<(uint, string, string)>(count);

        for (int i = 0; i < count; i++)
        {
            uint networkId = reader.GetUInt();
            string mesh = reader.GetString();
            string texture = reader.GetString();
            result.Add((networkId, mesh, texture));
        }

        return result;
    }
}
