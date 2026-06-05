using BGPLite.Configuration;

namespace BGPLite.Routing;

public sealed class AllowAllFilter : IRouteFilter
{
    public static AllowAllFilter Instance { get; } = new();

    public bool AcceptIncoming(Route route, PeerConfig peer) => true;
    public bool AcceptOutgoing(Route route, PeerConfig peer) => true;
}
