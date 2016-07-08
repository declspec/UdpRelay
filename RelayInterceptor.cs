public interface IRelayInterceptor {
    byte[] TransformIncoming(ArraySlice<byte> incoming);
    byte[] TransformOutgoing(ArraySlice<byte> outgoing);
    byte[] Handle(ArraySlice<byte> datagram);
}

public abstract class RelayInterceptor : IRelayInterceptor
{
    public virtual byte[] Handle(ArraySlice<byte> datagram)
    {
        return null;
    }

    public virtual byte[] TransformIncoming(ArraySlice<byte> incoming)
    {
        return null;
    }

    public virtual byte[] TransformOutgoing(ArraySlice<byte> outgoing)
    {
        return null;
    }
}