namespace BGPLite.Server;

public interface IPeerStore
{
    string CreatePeer(string ip, uint asn, string? description);
    void UpsertPeer(string ip, uint asn);
    void UpdateSessionStatus(string ip, bool active);
    PeerInfo? GetPeerByIp(string ip);
    List<string> GetSubscriptions(string peerId);
    List<string> GetCustomPrefixes(string peerId);
}

public class PeerInfo
{
    public string Id { get; init; } = "";
    public string Ip { get; init; } = "";
    public uint? Asn { get; init; }
    public string? Description { get; init; }
    public string Status { get; init; } = "inactive";
    public string CreatedAt { get; init; } = "";
    public string? LastSessionAt { get; init; }
}
