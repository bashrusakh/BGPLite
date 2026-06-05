using BGPLite.Protocol;

namespace BGPLite.Server;

public sealed class BgpTimers
{
    public TimeSpan KeepAliveInterval { get; init; } = TimeSpan.FromSeconds(BgpConstants.DefaultKeepAlive);
    public TimeSpan HoldTime { get; init; } = TimeSpan.FromSeconds(BgpConstants.DefaultHoldTime);
    public TimeSpan ConnectRetryInterval { get; init; } = TimeSpan.FromSeconds(BgpConstants.ConnectRetryDelay);
}
