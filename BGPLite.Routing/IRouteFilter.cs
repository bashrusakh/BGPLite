using BGPLite.Configuration;

namespace BGPLite.Routing;

public interface IRouteFilter
{
    bool AcceptIncoming(Route route, PeerConfig peer);
    bool AcceptOutgoing(Route route, PeerConfig peer);
}
