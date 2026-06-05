using BGPLite.Configuration;

namespace BGPLite.Routing;

public sealed class PeerCommunityFilter : IRouteFilter
{
    private readonly Func<string, HashSet<uint>> _getCommunities;

    public PeerCommunityFilter(Func<string, HashSet<uint>> getCommunities)
    {
        _getCommunities = getCommunities;
    }

    public bool AcceptIncoming(Route route, PeerConfig peer) => true;

    public bool AcceptOutgoing(Route route, PeerConfig peer)
    {
        var allowed = _getCommunities(peer.Address);
        if (allowed.Count == 0)
            return true; // no filter = all routes

        if (route.Communities.Length == 0)
            return true; // routes without community always pass

        foreach (var c in route.Communities)
        {
            if (allowed.Contains(c))
                return true;
        }

        return false;
    }
}
