using LiteNetLib.Utils;
using Silk.NET.Maths;

namespace MyEngine.Networking;

/// <summary>A client's current movement intent (world-axis direction, not raw key codes) sent every fixed tick.</summary>
public static class ClientInputMessage
{
    public static void Write(NetDataWriter writer, Vector3D<float> moveDirection)
    {
        writer.Put((byte)MessageType.ClientInput);
        writer.Put(moveDirection.X);
        writer.Put(moveDirection.Y);
        writer.Put(moveDirection.Z);
    }

    public static Vector3D<float> Read(NetDataReader reader)
    {
        return new Vector3D<float>(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
    }
}
