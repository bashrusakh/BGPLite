namespace BGPLite.Api.Entities;

public class PeerSubscription
{
    public string PeerId { get; set; } = "";
    public string AsnListName { get; set; } = "";

    public Peer Peer { get; set; } = null!;
}
