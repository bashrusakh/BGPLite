namespace BGPLite.Protocol;

public readonly record struct IpPrefix(uint Address, byte Length)
{
    public override string ToString() => $"{BgpConstants.UintToIPAddress(Address)}/{Length}";
}
