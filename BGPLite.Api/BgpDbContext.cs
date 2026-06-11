using BGPLite.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace BGPLite.Api;

public class BgpDbContext : DbContext
{
    public DbSet<Peer> Peers => Set<Peer>();

    public BgpDbContext(DbContextOptions<BgpDbContext> options) : base(options) { }

    public static void Initialize(BgpDbContext db)
    {
        db.Database.EnsureCreated();
        db.Database.ExecuteSqlRaw(
            "CREATE TABLE IF NOT EXISTS PeerCustomAsns (" +
            "PeerId TEXT NOT NULL, Asn INTEGER NOT NULL, " +
            "PRIMARY KEY (PeerId, Asn), " +
            "FOREIGN KEY (PeerId) REFERENCES Peers(Id) ON DELETE CASCADE)");
        db.Peers.Where(p => p.Status == "active").ExecuteUpdate(
            s => s.SetProperty(p => p.Status, "inactive"));
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Peer>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.Ip).IsUnique();
            e.Property(p => p.Status).HasDefaultValue("inactive");
        });

        model.Entity<PeerCommunity>(e =>
        {
            e.HasKey(c => new { c.PeerId, c.Community });
            e.HasOne(c => c.Peer).WithMany(p => p.Communities)
                .HasForeignKey(c => c.PeerId).OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<PeerSubscription>(e =>
        {
            e.HasKey(s => new { s.PeerId, s.AsnListName });
            e.HasOne(s => s.Peer).WithMany(p => p.Subscriptions)
                .HasForeignKey(s => s.PeerId).OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<PeerCustomPrefix>(e =>
        {
            e.HasKey(c => new { c.PeerId, c.Prefix, c.PrefixLength });
            e.HasOne(c => c.Peer).WithMany(p => p.CustomPrefixes)
                .HasForeignKey(c => c.PeerId).OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<PeerCustomAsn>(e =>
        {
            e.HasKey(c => new { c.PeerId, c.Asn });
            e.HasOne(c => c.Peer).WithMany(p => p.CustomAsns)
                .HasForeignKey(c => c.PeerId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
