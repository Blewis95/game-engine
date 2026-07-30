namespace MyEngine.Networking;

public enum MessageType : byte
{
    EntitySpawn = 1,
    WorldSnapshot = 2,
    ClientInput = 3,
    EntityDespawn = 4,
}
