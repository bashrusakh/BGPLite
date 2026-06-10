using BGPLite.Api.Entities;
using BGPLite.Server;
using Microsoft.EntityFrameworkCore;

namespace BGPLite.Api;

public sealed class PeerStore : IPeerStore
{
    private readonly IDbContextFactory<BgpDbContext> _dbFactory;

    public PeerStore(IDbContextFactory<BgpDbContext> dbFactory) => _dbFactory = dbFactory;

    public string CreatePeer(string ip, uint asn, string? description)
    {
        using var db = _dbFactory.CreateDbContext();
        var existing = db.Peers.FirstOrDefault(p => p.Ip == ip);
        if (existing is not null)
        {
            existing.Asn = asn;
            existing.Description = description;
            db.SaveChanges();
            return existing.Id;
        }

        var peer = new Peer { Ip = ip, Asn = asn, Description = description };
        db.Peers.Add(peer);
        db.SaveChanges();
        return peer.Id;
    }

    public void UpsertPeer(string ip, uint asn)
    {
        using var db = _dbFactory.CreateDbContext();
        var peer = db.Peers.FirstOrDefault(p => p.Ip == ip);
        if (peer is null)
        {
            db.Peers.Add(new Peer { Ip = ip, Asn = asn, Status = "active", LastSessionAt = DateTime.UtcNow });
        }
        else
        {
            peer.Asn = asn;
            peer.Status = "active";
            peer.LastSessionAt = DateTime.UtcNow;
        }
        db.SaveChanges();
    }

    public void UpdateSessionStatus(string ip, bool active)
    {
        using var db = _dbFactory.CreateDbContext();
        var peer = db.Peers.FirstOrDefault(p => p.Ip == ip);
        if (peer is null) return;

        peer.Status = active ? "active" : "inactive";
        if (active) peer.LastSessionAt = DateTime.UtcNow;
        db.SaveChanges();
    }

    public void DeletePeer(string id)
    {
        using var db = _dbFactory.CreateDbContext();
        db.Peers.Where(p => p.Id == id).ExecuteDelete();
    }

    public List<Peer> GetAllPeers()
    {
        using var db = _dbFactory.CreateDbContext();
        return db.Peers.Include(p => p.Communities).ToList();
    }

    public Peer? GetDbPeerById(string id)
    {
        using var db = _dbFactory.CreateDbContext();
        return db.Peers.Include(p => p.Communities).FirstOrDefault(p => p.Id == id);
    }

    PeerInfo? IPeerStore.GetPeerById(string id)
    {
        using var db = _dbFactory.CreateDbContext();
        var peer = db.Peers.Include(p => p.Communities).FirstOrDefault(p => p.Id == id);
        return peer is null ? null : MapToInfo(peer);
    }

    public PeerInfo? GetPeerByIp(string ip)
    {
        using var db = _dbFactory.CreateDbContext();
        var peer = db.Peers.Include(p => p.Communities).FirstOrDefault(p => p.Ip == ip);
        return peer is null ? null : MapToInfo(peer);
    }

    public void SetDescription(string id, string description)
    {
        using var db = _dbFactory.CreateDbContext();
        db.Peers.Where(p => p.Id == id).ExecuteUpdate(
            s => s.SetProperty(p => p.Description, description));
    }

    public HashSet<uint> GetCommunities(string peerId)
    {
        using var db = _dbFactory.CreateDbContext();
        return db.Peers.Include(p => p.Communities)
            .Where(p => p.Id == peerId)
            .SelectMany(p => p.Communities)
            .Select(c => (uint)c.Community)
            .ToHashSet();
    }

    public HashSet<uint> GetCommunitiesByIp(string ip)
    {
        using var db = _dbFactory.CreateDbContext();
        return db.Peers.Include(p => p.Communities)
            .Where(p => p.Ip == ip)
            .SelectMany(p => p.Communities)
            .Select(c => (uint)c.Community)
            .ToHashSet();
    }

    public void SetCommunities(string peerId, HashSet<uint> communities)
    {
        using var db = _dbFactory.CreateDbContext();
        db.Set<PeerCommunity>().Where(c => c.PeerId == peerId).ExecuteDelete();
        db.Set<PeerCommunity>().AddRange(
            communities.Select(c => new PeerCommunity { PeerId = peerId, Community = c }));
        db.SaveChanges();
    }

    public void ClearCommunities(string peerId)
    {
        using var db = _dbFactory.CreateDbContext();
        db.Set<PeerCommunity>().Where(c => c.PeerId == peerId).ExecuteDelete();
    }

    public List<string> GetSubscriptions(string peerId)
    {
        using var db = _dbFactory.CreateDbContext();
        return db.Set<PeerSubscription>()
            .Where(s => s.PeerId == peerId)
            .Select(s => s.AsnListName)
            .ToList();
    }

    public void SetSubscriptions(string peerId, List<string> asnListNames)
    {
        using var db = _dbFactory.CreateDbContext();
        db.Set<PeerSubscription>().Where(s => s.PeerId == peerId).ExecuteDelete();
        db.Set<PeerSubscription>().AddRange(
            asnListNames.Select(n => new PeerSubscription { PeerId = peerId, AsnListName = n }));
        db.SaveChanges();
    }

    public List<string> GetCustomPrefixes(string peerId)
    {
        using var db = _dbFactory.CreateDbContext();
        return db.Set<PeerCustomPrefix>()
            .Where(c => c.PeerId == peerId)
            .Select(c => c.Prefix + "/" + c.PrefixLength)
            .ToList();
    }

    public void SetCustomPrefixes(string peerId, List<(string Prefix, byte Length)> prefixes)
    {
        using var db = _dbFactory.CreateDbContext();
        db.Set<PeerCustomPrefix>().Where(c => c.PeerId == peerId).ExecuteDelete();
        db.Set<PeerCustomPrefix>().AddRange(
            prefixes.Select(p => new PeerCustomPrefix { PeerId = peerId, Prefix = p.Prefix, PrefixLength = p.Length }));
        db.SaveChanges();
    }

    private static PeerInfo MapToInfo(Peer peer) => new()
    {
        Id = peer.Id,
        Ip = peer.Ip,
        Asn = peer.Asn,
        Description = peer.Description,
        Status = peer.Status,
        CreatedAt = peer.CreatedAt.ToString("O"),
        LastSessionAt = peer.LastSessionAt?.ToString("O")
    };
}
