namespace BGPLite.Protocol;

public sealed class BgpUpdateMessage : BgpMessage
{
    public override BgpMessageType Type => BgpMessageType.Update;
    public List<IpPrefix> WithdrawnRoutes { get; init; } = [];
    public List<PathAttribute> PathAttributes { get; init; } = [];
    public List<IpPrefix> Nlri { get; init; } = [];
}
