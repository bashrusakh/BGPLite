namespace BGPLite.Api.Entities;

public class PeerCommunity
{
    public string PeerId { get; set; } = "";
    public long Community { get; set; }

    public Peer Peer { get; set; } = null!;
}
