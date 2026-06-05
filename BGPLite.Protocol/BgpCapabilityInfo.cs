using System.Buffers.Binary;

namespace BGPLite.Protocol;

public sealed class BgpCapabilityInfo
{
    public byte Code { get; init; }
    public byte[] Data { get; init; } = [];

    public static BgpCapabilityInfo FourOctetAsn(uint asn)
    {
        var data = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(data, asn);
        return new BgpCapabilityInfo { Code = BgpConstants.Capability.FourOctetAsn, Data = data };
    }

    public static BgpCapabilityInfo RouteRefresh() => new()
    {
        Code = BgpConstants.Capability.RouteRefresh
    };

    public static BgpCapabilityInfo MultiprotocolIpv4Unicast() => new()
    {
        Code = BgpConstants.Capability.Multiprotocol,
        Data = [(byte)(BgpConstants.Afi.IPv4 >> 8), (byte)BgpConstants.Afi.IPv4, 0x00, BgpConstants.Safi.Unicast]
    };

    public uint ReadAsn() => BinaryPrimitives.ReadUInt32BigEndian(Data);
}
