namespace BGPLite.Protocol;

public sealed class PathAttribute
{
    public byte Flags { get; init; }
    public byte TypeCode { get; init; }
    public byte[] Data { get; init; } = [];

    public bool Optional => (Flags & BgpConstants.Attribute.FlagOptional) != 0;
    public bool Transitive => (Flags & BgpConstants.Attribute.FlagTransitive) != 0;
    public bool Partial => (Flags & BgpConstants.Attribute.FlagPartial) != 0;
}
