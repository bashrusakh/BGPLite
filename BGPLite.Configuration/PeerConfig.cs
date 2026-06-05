using System.Net;
using YamlDotNet.Serialization;

namespace BGPLite.Configuration;

public sealed class PeerConfig
{
    [YamlMember(Alias = "Address")]
    public string Address { get; init; } = "0.0.0.0";

    [YamlMember(Alias = "RemoteAsn")]
    public uint? RemoteAsn { get; init; }

    [YamlMember(Alias = "Description")]
    public string? Description { get; init; }

    public IPAddress GetAddress() => IPAddress.Parse(Address);
}
