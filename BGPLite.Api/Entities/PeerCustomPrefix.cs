namespace BGPLite.Api.Entities;

public class PeerCustomPrefix
{
    public string PeerId { get; set; } = "";
    public string Prefix { get; set; } = "";
    public int PrefixLength { get; set; }

    public Peer Peer { get; set; } = null!;
}
