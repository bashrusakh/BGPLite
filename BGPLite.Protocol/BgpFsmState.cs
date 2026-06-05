namespace BGPLite.Protocol;

public enum BgpFsmState
{
    Idle,
    Connect,
    OpenSent,
    OpenConfirm,
    Established
}
