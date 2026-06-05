namespace BGPLite.Routing;

public sealed class Route
{
    public required uint Prefix { get; init; }
    public required byte PrefixLength { get; init; }
    public required uint NextHop { get; init; }
    public uint[] AsPath { get; init; } = [];
    public uint[] Communities { get; init; } = [];
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    public (uint Prefix, byte Length) Key => (Prefix, PrefixLength);
}
