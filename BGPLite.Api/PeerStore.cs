using BGPLite.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace BGPLite.Api;

public sealed class PeerStore
{
    private readonly BgpDbContext _db;

    public PeerStore(BgpDbContext db) => _db = db;

    public string CreatePeer(string ip, uint asn, string? description)
    {
        var peer = new Peer { Ip = ip, Asn = asn, Description = description };
        _db.Peers.Add(peer);
        _db.SaveChanges();
        return peer.Id;
    }

    public void UpsertPeer(string ip, uint asn)
    {
        var peer = _db.Peers.FirstOrDefault(p => p.Ip == ip);
        if (peer is null)
        {
            _db.Peers.Add(new Peer { Ip = ip, Asn = asn, Status = "active" });
        }
        else
        {
            peer.Asn = asn;
            peer.Status = "active";
            peer.LastSessionAt = DateTime.UtcNow;
        }
        _db.SaveChanges();
    }

    public void UpdateSessionStatus(string ip, bool active)
    {
        var peer = _db.Peers.FirstOrDefault(p => p.Ip == ip);
        if (peer is null) return;

        peer.Status = active ? "active" : "inactive";
        if (active) peer.LastSessionAt = DateTime.UtcNow;
        _db.SaveChanges();
    }

    public void DeletePeer(string id)
    {
        _db.Peers.Where(p => p.Id == id).ExecuteDelete();
    }

    public List<Peer> GetAllPeers() =>
        _db.Peers.Include(p => p.Communities).ToList();

    public Peer? GetPeerById(string id) =>
        _db.Peers.Include(p => p.Communities).FirstOrDefault(p => p.Id == id);

    public Peer? GetPeerByIp(string ip) =>
        _db.Peers.Include(p => p.Communities).FirstOrDefault(p => p.Ip == ip);

    public void SetDescription(string id, string description)
    {
        _db.Peers.Where(p => p.Id == id).ExecuteUpdate(
            s => s.SetProperty(p => p.Description, description));
    }

    public HashSet<uint> GetCommunities(string peerId) =>
        _db.Peers.Include(p => p.Communities)
            .Where(p => p.Id == peerId)
            .SelectMany(p => p.Communities)
            .Select(c => (uint)c.Community)
            .ToHashSet();

    public HashSet<uint> GetCommunitiesByIp(string ip) =>
        _db.Peers.Include(p => p.Communities)
            .Where(p => p.Ip == ip)
            .SelectMany(p => p.Communities)
            .Select(c => (uint)c.Community)
            .ToHashSet();

    public void SetCommunities(string peerId, HashSet<uint> communities)
    {
        _db.Set<PeerCommunity>().Where(c => c.PeerId == peerId).ExecuteDelete();
        _db.Set<PeerCommunity>().AddRange(
            communities.Select(c => new PeerCommunity { PeerId = peerId, Community = c }));
        _db.SaveChanges();
    }

    public void ClearCommunities(string peerId)
    {
        _db.Set<PeerCommunity>().Where(c => c.PeerId == peerId).ExecuteDelete();
    }

    public List<string> GetSubscriptions(string peerId) =>
        _db.Set<PeerSubscription>()
            .Where(s => s.PeerId == peerId)
            .Select(s => s.AsnListName)
            .ToList();

    public void SetSubscriptions(string peerId, List<string> asnListNames)
    {
        _db.Set<PeerSubscription>().Where(s => s.PeerId == peerId).ExecuteDelete();
        _db.Set<PeerSubscription>().AddRange(
            asnListNames.Select(n => new PeerSubscription { PeerId = peerId, AsnListName = n }));
        _db.SaveChanges();
    }

    public List<string> GetCustomPrefixes(string peerId) =>
        _db.Set<PeerCustomPrefix>()
            .Where(c => c.PeerId == peerId)
            .Select(c => c.Prefix + "/" + c.PrefixLength)
            .ToList();

    public void SetCustomPrefixes(string peerId, List<(string Prefix, byte Length)> prefixes)
    {
        _db.Set<PeerCustomPrefix>().Where(c => c.PeerId == peerId).ExecuteDelete();
        _db.Set<PeerCustomPrefix>().AddRange(
            prefixes.Select(p => new PeerCustomPrefix { PeerId = peerId, Prefix = p.Prefix, PrefixLength = p.Length }));
        _db.SaveChanges();
    }
}
