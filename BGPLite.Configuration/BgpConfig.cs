using System.Net;
using YamlDotNet.Serialization;

namespace BGPLite.Configuration;

public sealed class BgpConfig
{
    [YamlMember(Alias = "Asn")]
    public uint Asn { get; init; }

    [YamlMember(Alias = "RouterId")]
    public string RouterId { get; init; } = "0.0.0.0";

    [YamlMember(Alias = "KeepAlive")]
    public int KeepAlive { get; init; } = 60;

    [YamlMember(Alias = "HoldTime")]
    public int HoldTime { get; init; } = 180;

    public IPAddress GetRouterIdAddress() => IPAddress.Parse(RouterId);
}
