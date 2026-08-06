using LiteNetLib.Utils;
using Silk.NET.Maths;

namespace MyEngine.Networking;

/// <summary>
/// A client's current movement intent (world-axis direction, not raw key
/// codes) sent every fixed tick. Sequence increases by one per input sent -
/// it lets the server tell the client (via WorldSnapshotMessage) which
/// inputs it has already applied, for prediction/reconciliation. Jump is a
/// continuous key-held flag, not edge-triggered - the server gates actually
/// jumping on being grounded, so holding it just lets you hop repeatedly
/// rather than fly.
/// </summary>
public static class ClientInputMessage
{
    public static void Write(NetDataWriter writer, uint sequence, Vector3D<float> moveDirection, bool jump)
    {
        writer.Put((byte)MessageType.ClientInput);
        writer.Put(sequence);
        writer.Put(moveDirection.X);
        writer.Put(moveDirection.Y);
        writer.Put(moveDirection.Z);
        writer.Put(jump);
    }

    public static (uint Sequence, Vector3D<float> MoveDirection, bool Jump) Read(NetDataReader reader)
    {
        uint sequence = reader.GetUInt();
        var direction = new Vector3D<float>(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        bool jump = reader.GetBool();
        return (sequence, direction, jump);
    }
}
