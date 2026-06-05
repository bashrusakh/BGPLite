using System.Threading;

namespace BGPLite.Server;

public sealed class BgpMetrics
{
    private int _peerCount;
    private int _routeCount;
    private long _updatesReceived;
    private long _updatesSent;
    private int _activeSessions;

    public int PeerCount => Volatile.Read(ref _peerCount);
    public int RouteCount => Volatile.Read(ref _routeCount);
    public long UpdatesReceived => Interlocked.Read(ref _updatesReceived);
    public long UpdatesSent => Interlocked.Read(ref _updatesSent);
    public int ActiveSessions => Volatile.Read(ref _activeSessions);

    public void PeerConnected() => Interlocked.Increment(ref _peerCount);
    public void PeerDisconnected() => Interlocked.Decrement(ref _peerCount);
    public void UpdateReceived() => Interlocked.Increment(ref _updatesReceived);
    public void UpdateSent() => Interlocked.Increment(ref _updatesSent);
    public void SessionEstablished() => Interlocked.Increment(ref _activeSessions);
    public void SessionClosed() => Interlocked.Decrement(ref _activeSessions);
    public void SetRouteCount(int count) => Volatile.Write(ref _routeCount, count);
}
