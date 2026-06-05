namespace BGPLite.Protocol;

public abstract class BgpMessage
{
    public abstract BgpMessageType Type { get; }
}
