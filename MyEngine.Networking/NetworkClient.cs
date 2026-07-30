using LiteNetLib;

namespace MyEngine.Networking;

/// <summary>Wraps LiteNetLib's client side so the library stays an implementation detail, same as Renderer/GameLoop wrapping Silk.NET.</summary>
public sealed class NetworkClient : IDisposable
{
    private readonly EventBasedNetListener _listener = new();
    private readonly NetManager _netManager;

    public NetPeer? Server { get; private set; }
    public event Action<NetPeer>? Connected;
    public event Action? Disconnected;

    public NetworkClient()
    {
        _netManager = new NetManager(_listener);

        _listener.PeerConnectedEvent += peer =>
        {
            Server = peer;
            Connected?.Invoke(peer);
        };
        _listener.PeerDisconnectedEvent += (_, _) =>
        {
            Server = null;
            Disconnected?.Invoke();
        };
    }

    public void Connect(string address, int port = NetworkConfig.DefaultPort)
    {
        _netManager.Start();
        _netManager.Connect(address, port, NetworkConfig.ConnectionKey);
    }

    /// <summary>Call once per loop iteration to process incoming events/callbacks.</summary>
    public void PollEvents() => _netManager.PollEvents();

    public void Dispose() => _netManager.Stop();
}
