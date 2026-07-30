using LiteNetLib;

namespace MyEngine.Networking;

/// <summary>Wraps LiteNetLib's server side so the library stays an implementation detail, same as Renderer/GameLoop wrapping Silk.NET.</summary>
public sealed class NetworkServer : IDisposable
{
    private readonly EventBasedNetListener _listener = new();
    private readonly NetManager _netManager;

    public event Action<NetPeer>? ClientConnected;
    public event Action<NetPeer>? ClientDisconnected;

    public NetworkServer()
    {
        _netManager = new NetManager(_listener);

        _listener.ConnectionRequestEvent += request => request.AcceptIfKey(NetworkConfig.ConnectionKey);
        _listener.PeerConnectedEvent += peer => ClientConnected?.Invoke(peer);
        _listener.PeerDisconnectedEvent += (peer, _) => ClientDisconnected?.Invoke(peer);
    }

    public void Start(int port = NetworkConfig.DefaultPort) => _netManager.Start(port);

    /// <summary>Call once per loop iteration to process incoming events/callbacks.</summary>
    public void PollEvents() => _netManager.PollEvents();

    public void Dispose() => _netManager.Stop();
}
