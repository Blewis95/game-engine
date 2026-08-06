using MyEngine.ECS.Components;
using Silk.NET.Maths;

namespace MyEngine.Networking;

/// <summary>
/// Buffers local inputs that have been sent but not yet acknowledged by the
/// server. Reconcile() drops acknowledged entries and replays whatever's
/// left on top of the server's authoritative Transform, reconstructing the
/// client's predicted present-time state so a correction (when prediction
/// and server truth agree, which is the common case) is invisible.
/// </summary>
public sealed class PredictedMovement
{
    private readonly List<(uint Sequence, Vector3D<float> Direction)> _pending = new();

    public uint NextSequence { get; private set; }

    /// <summary>Buffers the input and returns the sequence number to send alongside it.</summary>
    public uint RecordInput(Vector3D<float> direction)
    {
        uint sequence = NextSequence++;
        _pending.Add((sequence, direction));
        return sequence;
    }

    public Transform Reconcile(Transform authoritative, float speed, double fixedDeltaTime, uint lastProcessedSequence)
    {
        _pending.RemoveAll(input => input.Sequence <= lastProcessedSequence);

        var transform = authoritative;
        foreach (var (_, direction) in _pending)
            transform.Position += direction * speed * (float)fixedDeltaTime;

        return transform;
    }
}
