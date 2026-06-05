namespace BGPLite.Protocol;

public sealed class BgpOpenMessage : BgpMessage
{
    public override BgpMessageType Type => BgpMessageType.Open;
    public byte Version { get; init; } = BgpConstants.BgpVersion;
    public ushort Asn { get; init; }
    public ushort HoldTime { get; init; }
    public uint RouterId { get; init; }
    public List<BgpCapabilityInfo> Capabilities { get; init; } = [];
}
