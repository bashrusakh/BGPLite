using System.Numerics;

namespace BGPLite.Routing;

/// <summary>
/// Default <see cref="IPrefixAggregator"/>. Merges adjacent/overlapping IPv4 prefixes
/// into the minimal equivalent set whose address range is EXACTLY the union of the
/// inputs — never a single address more. Injectable as a strategy; inject
/// <see cref="NoOpPrefixAggregator"/> to disable summarization.
/// </summary>
/// <remarks>
/// Algorithm: (1) mask host bits and form inclusive <c>[start, end]</c> intervals,
/// (2) sort and merge overlapping/adjacent intervals into a disjoint union, (3) emit the
/// minimal exact CIDR cover of each merged interval (range→CIDR). Because step 3 only
/// produces a /N block when its full span is present, it can never announce addresses
/// that were not in the input.
/// </remarks>
public sealed class ExactUnionPrefixAggregator : IPrefixAggregator
{
    public IReadOnlyList<Route> Aggregate(IEnumerable<Route> routes)
    {
        var source = routes as List<Route> ?? routes.ToList();
        if (source.Count == 0)
            return source;

        var result = new List<Route>(source.Count);

        // Group by the attributes that survive to the wire. The outgoing path rewrites
        // AS_PATH (to the local ASN) and NEXT_HOP, so only Communities distinguish
        // otherwise-mergeable prefixes; prefixes carrying different communities stay in
        // separate groups so community information is never mixed during merging.
        foreach (var group in source.GroupBy(AttributeKey.From))
        {
            var template = group.First();
            foreach (var (prefix, length) in AggregatePrefixes(group.Select(r => (r.Prefix, r.PrefixLength))))
            {
                result.Add(new Route
                {
                    Prefix = prefix,
                    PrefixLength = length,
                    NextHop = template.NextHop,
                    Communities = template.Communities
                });
            }
        }

        return result;
    }

    /// <summary>Exact-union CIDR merge of raw (prefix, length) pairs.</summary>
    private static List<(uint Prefix, byte Length)> AggregatePrefixes(IEnumerable<(uint Prefix, byte Length)> prefixes)
    {
        // 1. Mask host bits and build inclusive [start, end] intervals. ulong so a /0 fits.
        var intervals = new List<(ulong Start, ulong End)>();
        foreach (var (prefix, length) in prefixes)
        {
            if (length > 32) continue; // defensive: skip malformed prefixes
            var mask = length == 0 ? 0u : (0xFFFFFFFFu << (32 - length));
            var start = (ulong)(prefix & mask);
            var size = length == 0 ? (1UL << 32) : (1UL << (32 - length));
            intervals.Add((start, start + size - 1));
        }
        if (intervals.Count == 0)
            return [];

        // 2. Sort and merge overlapping/adjacent intervals into a disjoint union.
        intervals.Sort((a, b) => a.Start.CompareTo(b.Start));
        var merged = new List<(ulong Start, ulong End)>(intervals.Count) { intervals[0] };
        for (var i = 1; i < intervals.Count; i++)
        {
            var (start, end) = intervals[i];
            var last = merged[^1];
            if (start <= last.End + 1) // overlap or directly adjacent
            {
                if (end > last.End) merged[^1] = (last.Start, end);
            }
            else
            {
                merged.Add((start, end));
            }
        }

        // 3. Emit the minimal exact CIDR cover for each merged interval.
        var result = new List<(uint, byte)>();
        foreach (var (start, end) in merged)
            EmitRange(start, end, result);
        return result;
    }

    /// <summary>
    /// Emits the fewest CIDR blocks that exactly cover the inclusive <paramref name="start"/>
    /// .. <paramref name="end"/> range. At each step the block is the largest power-of-two
    /// that is both aligned at <c>start</c> and fits inside the remaining range, so the
    /// emitted blocks tile the range with neither gaps nor overlaps.
    /// </summary>
    private static void EmitRange(ulong start, ulong end, List<(uint, byte)> result)
    {
        while (start <= end)
        {
            var aligned = start == 0 ? (1UL << 32) : start & (~start + 1); // largest pow2 dividing start
            var fits = HighestPowerOfTwo(end - start + 1);                 // largest pow2 ≤ remaining
            var size = Math.Min(aligned, fits);
            result.Add(((uint)start, (byte)(32 - BitOperations.Log2(size))));
            start += size;
        }
    }

    private static ulong HighestPowerOfTwo(ulong value) =>
        1UL << (63 - BitOperations.LeadingZeroCount(value));

    /// <summary>Value-equality key over a route's communities (sorted, set semantics).</summary>
    private readonly struct AttributeKey : IEquatable<AttributeKey>
    {
        private readonly uint[] _communities;

        private AttributeKey(uint[] communities) => _communities = communities;

        public static AttributeKey From(Route route)
        {
            var c = route.Communities;
            if (c.Length <= 1) return new AttributeKey(c);
            // Communities are a set: dedup (and sort) so set-equivalent routes key together.
            var sorted = c.Distinct().ToArray();
            Array.Sort(sorted);
            return new AttributeKey(sorted);
        }

        public bool Equals(AttributeKey other)
        {
            if (_communities.Length != other._communities.Length) return false;
            for (var i = 0; i < _communities.Length; i++)
                if (_communities[i] != other._communities[i]) return false;
            return true;
        }

        public override bool Equals(object? obj) => obj is AttributeKey other && Equals(other);

        public override int GetHashCode()
        {
            var hc = new HashCode();
            foreach (var c in _communities) hc.Add(c);
            return hc.ToHashCode();
        }
    }
}
