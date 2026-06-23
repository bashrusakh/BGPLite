namespace BGPLite.Routing;

/// <summary>
/// Summarizes the routes advertised to a peer by merging adjacent/overlapping prefixes.
/// Applied to the outgoing set in <see cref="Server.BgpSession"/> just before batching.
/// </summary>
/// <remarks>
/// Implementations MUST NOT introduce any address outside the union of the inputs —
/// only exact merging is permitted (no over-claiming). Two sibling /24s collapse to a
/// /23 only when they are the two aligned halves of that /23; a /24 nested inside a /22
/// is absorbed, never extended.
/// <para>
/// Implementations MUST be stateless and thread-safe: a single instance may be shared
/// across concurrent sessions.
/// </para>
/// </remarks>
public interface IPrefixAggregator
{
    /// <summary>
    /// Returns the aggregated route set. May return the input verbatim. Prefixes that
    /// merge carry the attributes of their attribute-group (see the default implementation).
    /// </summary>
    IReadOnlyList<Route> Aggregate(IEnumerable<Route> routes);
}
