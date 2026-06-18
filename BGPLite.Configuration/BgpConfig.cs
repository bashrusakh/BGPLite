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

    /// <summary>Advertise Graceful Restart (RFC 4724) and send an End-of-RIB marker after the
    /// initial route dump, so GR-capable peers retain our routes across our restart.</summary>
    [YamlMember(Alias = "GracefulRestart")]
    public bool GracefulRestart { get; init; } = true;

    /// <summary>Restart Time (seconds) advertised in the GR capability. Clamped to
    /// min(HoldTime, 4095) — the RFC 4724 §2.2 field is 12 bits.</summary>
    [YamlMember(Alias = "RestartTime")]
    public int RestartTime { get; init; } = 120;

    /// <summary>Forwarding State (F) bit for IPv4/Unicast. When true, GR-capable peers keep our
    /// stale routes through the restart window (smoothest — prefixes never visibly disappear).
    /// Only keep true if the deployment preserves forwarding at the advertised next-hop (the
    /// router-id); otherwise a peer could forward to a non-forwarding speaker during the window.
    /// See RFC 4724 §3/§4.2.</summary>
    [YamlMember(Alias = "GracefulRestartForwardingState")]
    public bool GracefulRestartForwardingState { get; init; } = true;

    public IPAddress GetRouterIdAddress() => IPAddress.Parse(RouterId);
}
