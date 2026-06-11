namespace BGPLite.Api.Entities;

public class PeerCustomAsn
{
    public string PeerId { get; set; } = "";
    public uint Asn { get; set; }

    public Peer Peer { get; set; } = null!;
}
