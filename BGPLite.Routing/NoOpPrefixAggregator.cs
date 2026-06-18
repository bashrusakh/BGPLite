namespace BGPLite.Routing;

/// <summary>
/// Pass-through aggregator: returns the input verbatim. Inject this (instead of
/// <see cref="ExactUnionPrefixAggregator"/>) to disable prefix summarization.
/// </summary>
public sealed class NoOpPrefixAggregator : IPrefixAggregator
{
    public IReadOnlyList<Route> Aggregate(IEnumerable<Route> routes) =>
        routes as IReadOnlyList<Route> ?? routes.ToList();
}
