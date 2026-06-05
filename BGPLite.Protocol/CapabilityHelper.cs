using System.Buffers.Binary;

namespace BGPLite.Protocol;

public static class CapabilityHelper
{
    public static uint? GetRemoteAsn(BgpOpenMessage open)
    {
        foreach (var cap in open.Capabilities)
        {
            if (cap.Code == BgpConstants.Capability.FourOctetAsn && cap.Data.Length >= 4)
                return BinaryPrimitives.ReadUInt32BigEndian(cap.Data);
        }
        return null;
    }

    public static bool SupportsRouteRefresh(BgpOpenMessage open)
    {
        foreach (var cap in open.Capabilities)
        {
            if (cap.Code == BgpConstants.Capability.RouteRefresh)
                return true;
        }
        return false;
    }

    public static bool SupportsMultiprotocolIpv4Unicast(BgpOpenMessage open)
    {
        foreach (var cap in open.Capabilities)
        {
            if (cap.Code == BgpConstants.Capability.Multiprotocol &&
                cap.Data.Length >= 4 &&
                BinaryPrimitives.ReadUInt16BigEndian(cap.Data) == BgpConstants.Afi.IPv4 &&
                cap.Data[3] == BgpConstants.Safi.Unicast)
                return true;
        }
        return false;
    }

    public static uint GetEffectiveAsn(BgpOpenMessage open)
    {
        return GetRemoteAsn(open) ?? open.Asn;
    }
}
