using System;
using System.Net;
using System.Net.Sockets;

public class UdpRelay : IDisposable {
    private readonly EndPoint _localEndpoint;
    private readonly EndPoint _targetEndpoint;
    private readonly IRelayInterceptor _interceptor;

    private readonly Socket _master;
    private readonly Socket _slave;

    private bool _disposed;

     public UdpRelay(EndPoint local, EndPoint target)
        : this(local, target, null) { }

    public UdpRelay(EndPoint local, EndPoint target, IRelayInterceptor interceptor) {
        _localEndpoint = local;
        _targetEndpoint = target;
        _interceptor = interceptor;

        _master = new Socket(local.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        _slave = new Socket(target.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        _disposed = false;
    }

    public UdpRelay Bind() {
        _master.Bind(_localEndpoint);
        _slave.Bind(new IPEndPoint(IPAddress.Any, 0));

        return this;
    }

    public int RelayPacket(byte[] buffer) {
        if (_disposed)
            throw new InvalidOperationException("Cannot perform this action; relay has been disposed");

        var client = (EndPoint)(new IPEndPoint(IPAddress.Any, 0));
        var len = _master.ReceiveFrom(buffer, ref client);
        var incoming = new ArraySlice<byte>(buffer, 0, len);

        // Run the incoming datagram through the interceptor's 'Incoming' transformer
        // to apply any transformations required prior to passing the datagram on
        var transformed = _interceptor != null ? _interceptor.TransformIncoming(incoming) : null;
        var forward = transformed != null ? new ArraySlice<byte>(transformed) : incoming;

        // If the interceptor handled the datagram, then we do not relay the request
        // to the target EndPoint. This allows for the interceptor to implement its own
        // response to certain datagrams and avoid contacting the target EP.
        var overwritten = _interceptor != null ? _interceptor.Handle(forward) : null;
        
        var outgoing = overwritten != null
            ? new ArraySlice<byte>(overwritten)
            : Relay(forward, buffer);

        transformed = _interceptor != null ? _interceptor.TransformOutgoing(outgoing) : null;
        if (transformed != null)
            outgoing = new ArraySlice<byte>(transformed);

        return _master.SendTo(outgoing.Array, outgoing.Offset, outgoing.Length, SocketFlags.None, client);
    }

    private ArraySlice<byte> Relay(ArraySlice<byte> outgoing, byte[] buffer) {
        _slave.SendTo(outgoing.Array, outgoing.Offset, outgoing.Length, SocketFlags.None, _targetEndpoint);
        return new ArraySlice<byte>(buffer, 0, _slave.Receive(buffer));
    }

    public void Dispose() {
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing) {
        if (!_disposed) {
            if (disposing) {
                _master.Dispose();
                _slave.Dispose();
            }
            _disposed = true;
        }
    } 
}