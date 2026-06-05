using Microsoft.Data.Sqlite;

namespace BGPLite.Api;

public sealed class PeerStore : IDisposable
{
    private readonly SqliteConnection _connection;

    public PeerStore(string dbPath)
    {
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        InitSchema();
    }

    private void InitSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS peers (
                ip TEXT PRIMARY KEY,
                asn INTEGER,
                description TEXT,
                connected_at TEXT
            );

            CREATE TABLE IF NOT EXISTS peer_communities (
                peer_ip TEXT NOT NULL,
                community INTEGER NOT NULL,
                PRIMARY KEY (peer_ip, community),
                FOREIGN KEY (peer_ip) REFERENCES peers(ip) ON DELETE CASCADE
            )
            """;
        cmd.ExecuteNonQuery();
    }

    public void UpsertPeer(string ip, uint asn)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO peers (ip, asn, connected_at)
            VALUES ($ip, $asn, $now)
            ON CONFLICT(ip) DO UPDATE SET asn=$asn, connected_at=$now
            """;
        cmd.Parameters.AddWithValue("$ip", ip);
        cmd.Parameters.AddWithValue("$asn", (long)asn);
        cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public List<PeerInfo> GetAllPeers()
    {
        var peers = new List<PeerInfo>();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT ip, asn, description, connected_at FROM peers";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            peers.Add(new PeerInfo
            {
                Ip = reader.GetString(0),
                Asn = reader.IsDBNull(1) ? null : (uint?)reader.GetInt64(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                ConnectedAt = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }

        foreach (var peer in peers)
            peer.Communities = GetCommunities(peer.Ip);

        return peers;
    }

    public PeerInfo? GetPeer(string ip)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT ip, asn, description, connected_at FROM peers WHERE ip=$ip";
        cmd.Parameters.AddWithValue("$ip", ip);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        var peer = new PeerInfo
        {
            Ip = reader.GetString(0),
            Asn = reader.IsDBNull(1) ? null : (uint?)reader.GetInt64(1),
            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
            ConnectedAt = reader.IsDBNull(3) ? null : reader.GetString(3),
            Communities = GetCommunities(ip)
        };
        return peer;
    }

    public HashSet<uint> GetCommunities(string ip)
    {
        var communities = new HashSet<uint>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT community FROM peer_communities WHERE peer_ip=$ip";
        cmd.Parameters.AddWithValue("$ip", ip);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            communities.Add((uint)reader.GetInt64(0));
        return communities;
    }

    public void SetCommunities(string ip, HashSet<uint> communities)
    {
        using var tx = _connection.BeginTransaction();
        try
        {
            using (var del = _connection.CreateCommand())
            {
                del.CommandText = "DELETE FROM peer_communities WHERE peer_ip=$ip";
                del.Parameters.AddWithValue("$ip", ip);
                del.ExecuteNonQuery();
            }

            foreach (var c in communities)
            {
                using var ins = _connection.CreateCommand();
                ins.CommandText = "INSERT INTO peer_communities (peer_ip, community) VALUES ($ip, $c)";
                ins.Parameters.AddWithValue("$ip", ip);
                ins.Parameters.AddWithValue("$c", (long)c);
                ins.ExecuteNonQuery();
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void ClearCommunities(string ip)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM peer_communities WHERE peer_ip=$ip";
        cmd.Parameters.AddWithValue("$ip", ip);
        cmd.ExecuteNonQuery();
    }

    public void SetDescription(string ip, string description)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "UPDATE peers SET description=$desc WHERE ip=$ip";
        cmd.Parameters.AddWithValue("$desc", description);
        cmd.Parameters.AddWithValue("$ip", ip);
        cmd.ExecuteNonQuery();
    }

    public void Dispose() => _connection.Dispose();
}

public class PeerInfo
{
    public string Ip { get; init; } = "";
    public uint? Asn { get; init; }
    public string? Description { get; init; }
    public string? ConnectedAt { get; init; }
    public HashSet<uint> Communities { get; set; } = [];
}
