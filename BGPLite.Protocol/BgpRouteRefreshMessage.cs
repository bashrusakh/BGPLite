namespace BGPLite.Protocol;

public sealed class BgpRouteRefreshMessage : BgpMessage
{
    public override BgpMessageType Type => BgpMessageType.RouteRefresh;
    public ushort Afi { get; init; }
    public byte Reserved { get; init; }
    public byte Safi { get; init; }
}
