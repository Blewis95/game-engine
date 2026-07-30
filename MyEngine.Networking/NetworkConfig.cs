namespace MyEngine.Networking;

public static class NetworkConfig
{
    public const int DefaultPort = 7777;

    /// <summary>LiteNetLib connection handshake key — rejects connections from anything that isn't this engine.</summary>
    public const string ConnectionKey = "MyEngine";
}
