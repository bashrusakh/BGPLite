namespace BGPLite.Protocol;

public enum BgpOrigin : byte
{
    Igp = 0,
    Egp = 1,
    Incomplete = 2
}
