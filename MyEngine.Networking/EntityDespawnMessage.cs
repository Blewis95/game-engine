using LiteNetLib.Utils;

namespace MyEngine.Networking;

/// <summary>Tells clients a replicated entity no longer exists (e.g. a player disconnected).</summary>
public static class EntityDespawnMessage
{
    public static void Write(NetDataWriter writer, uint networkId)
    {
        writer.Put((byte)MessageType.EntityDespawn);
        writer.Put(networkId);
    }

    public static uint Read(NetDataReader reader) => reader.GetUInt();
}
