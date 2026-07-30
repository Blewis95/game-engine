using LiteNetLib;
using LiteNetLib.Utils;

namespace MyEngine.Networking;

/// <summary>Wraps LiteNetLib's server side so the library stays an implementation detail, same as Renderer/GameLoop wrapping Silk.NET.</summary>
public sealed class NetworkServer : IDisposable
{
    private readonly EventBasedNetListener _listener = new();
    private readonly NetManager _netManager;
    private readonly HashSet<NetPeer> _connectedPeers = new();

    public event Action<NetPeer>? ClientConnected;
    public event Action<NetPeer>? ClientDisconnected;
    public event Action<NetPeer, NetPacketReader>? MessageReceived;

    public IReadOnlyCollection<NetPeer> ConnectedPeers => _connectedPeers;

    public NetworkServer()
    {
        _netManager = new NetManager(_listener);

        _listener.ConnectionRequestEvent += request => request.AcceptIfKey(NetworkConfig.ConnectionKey);
        _listener.PeerConnectedEvent += peer =>
        {
            _connectedPeers.Add(peer);
            ClientConnected?.Invoke(peer);
        };
        _listener.PeerDisconnectedEvent += (peer, _) =>
        {
            _connectedPeers.Remove(peer);
            ClientDisconnected?.Invoke(peer);
        };
        _listener.NetworkReceiveEvent += (peer, reader, _, _) =>
        {
            MessageReceived?.Invoke(peer, reader);
            reader.Recycle();
        };
    }

    public void Start(int port = NetworkConfig.DefaultPort) => _netManager.Start(port);

    /// <summary>Call once per loop iteration to process incoming events/callbacks.</summary>
    public void PollEvents() => _netManager.PollEvents();

    public void Send(NetPeer peer, NetDataWriter writer, DeliveryMethod deliveryMethod) =>
        peer.Send(writer, deliveryMethod);

    public void SendToAll(NetDataWriter writer, DeliveryMethod deliveryMethod) =>
        _netManager.SendToAll(writer, deliveryMethod);

    public void Dispose() => _netManager.Stop();
}
