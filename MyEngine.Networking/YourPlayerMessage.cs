using LiteNetLib.Utils;

namespace MyEngine.Networking;

/// <summary>
/// Tells a newly-connected client which replicated entity is its own (by
/// NetworkId) and that entity's movement speed, so the client can drive
/// local prediction for it. Sent once, right after that client's initial
/// spawn batch.
/// </summary>
public static class YourPlayerMessage
{
    public static void Write(NetDataWriter writer, uint networkId, float speed)
    {
        writer.Put((byte)MessageType.YourPlayer);
        writer.Put(networkId);
        writer.Put(speed);
    }

    public static (uint NetworkId, float Speed) Read(NetDataReader reader)
    {
        return (reader.GetUInt(), reader.GetFloat());
    }
}
