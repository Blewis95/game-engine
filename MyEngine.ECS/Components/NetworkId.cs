namespace MyEngine.ECS.Components;

/// <summary>
/// Stable cross-process identity for a replicated entity. Entity.Id is only
/// meaningful within one process's World, so networking needs this instead
/// to correlate the same logical entity between a server and its clients.
/// </summary>
public struct NetworkId
{
    public uint Value;

    public NetworkId(uint value) => Value = value;
}
