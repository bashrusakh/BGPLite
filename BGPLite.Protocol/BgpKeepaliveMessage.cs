namespace BGPLite.Protocol;

public sealed class BgpKeepaliveMessage : BgpMessage
{
    public override BgpMessageType Type => BgpMessageType.Keepalive;
    public static BgpKeepaliveMessage Instance { get; } = new();
}
