using BGPLite.Protocol;
using BGPLite.Routing;
using BGPLite.Server;

namespace BGPLite.Tests;

/// <summary>
/// Tests for RFC 6793 compliance — AS4_PATH handling for 2-byte-only peers.
/// </summary>
public class BgpSessionRfc6793Tests
{
    [Fact]
    public void GroupByCommunitySet_GroupsRoutesWithEmptyCommunitySet()
    {
        // Verify that BgpSession.GroupByCommunitySet works correctly
        // (static method, no session state needed)
        var routes = new List<Route>
        {
            new() { Prefix = 0xC0A80000, PrefixLength = 24, Communities = [] },
            new() { Prefix = 0x0A000000, PrefixLength = 8, Communities = [] }
        };

        var groups = BgpSession.GroupByCommunitySet(routes);
        Assert.Single(groups); // All routes share empty community set
        Assert.Equal(2, groups[0].Count);
    }

    [Fact]
    public void BuildAsPathAttributes_4BytePeer_ProducesOnlyAsPath()
    {
        // 4-byte peer: only 4-byte AS_PATH, no AS4_PATH
        var attrs = BgpSession.BuildAsPathAttributes(localAsn: 200000u, localFourByteAsn: true);

        var asPathAttr = Assert.Single(attrs);
        Assert.Equal(BgpConstants.Attribute.AsPath, asPathAttr.TypeCode);
        var ases = AttributeHelper.ReadAsPath(asPathAttr, fourByteAsn: true);
        Assert.Equal([200000u], ases);
    }

    [Fact]
    public void BuildAsPathAttributes_2BytePeer_LargeAsn_AsTransPlusAs4Path()
    {
        // 2-byte-only peer + local ASN > 65535: 2-byte AS_PATH with AS_TRANS + AS4_PATH
        var attrs = BgpSession.BuildAsPathAttributes(localAsn: 200000u, localFourByteAsn: false);

        Assert.Equal(2, attrs.Count);

        var asPathAttr = attrs[0];
        Assert.Equal(BgpConstants.Attribute.AsPath, asPathAttr.TypeCode);
        var asPathAses = AttributeHelper.ReadAsPath(asPathAttr, fourByteAsn: false);
        Assert.Equal([BgpConstants.AsPath.AsTrans], asPathAses);

        var as4PathAttr = attrs[1];
        Assert.Equal(BgpConstants.Attribute.As4Path, as4PathAttr.TypeCode);
        var as4PathAses = AttributeHelper.ReadAs4Path(as4PathAttr);
        Assert.Equal([200000u], as4PathAses);
    }

    [Fact]
    public void BuildAsPathAttributes_2BytePeer_2ByteAsn_AsPathOnly()
    {
        // 2-byte-only peer + local ASN <= 65535: 2-byte AS_PATH only, no AS4_PATH
        var attrs = BgpSession.BuildAsPathAttributes(localAsn: 65001u, localFourByteAsn: false);

        var asPathAttr = Assert.Single(attrs);
        Assert.Equal(BgpConstants.Attribute.AsPath, asPathAttr.TypeCode);
        var ases = AttributeHelper.ReadAsPath(asPathAttr, fourByteAsn: false);
        Assert.Equal([65001u], ases);
    }

    [Fact]
    public void BuildUpdateAttributes_2BytePeer_LargeAsn_PlacesCommunityBeforeAs4Path()
    {
        var attrs = BgpSession.BuildUpdateAttributes(
            localAsn: 200000u,
            localFourByteAsn: false,
            nextHop: 0xC0A80101,
            communities: [0x0000FF01]);

        Assert.Equal(
            [
                BgpConstants.Attribute.Origin,
                BgpConstants.Attribute.AsPath,
                BgpConstants.Attribute.NextHop,
                BgpConstants.Attribute.Community,
                BgpConstants.Attribute.As4Path
            ],
            attrs.Select(a => a.TypeCode).ToArray());

        Assert.Equal([BgpConstants.AsPath.AsTrans], AttributeHelper.ReadAsPath(attrs[1], fourByteAsn: false));
        Assert.Equal([200000u], AttributeHelper.ReadAs4Path(attrs[4]));
    }

    [Fact]
    public void MergeAsPathWithAs4Path_ReconstructsTrueSequence()
    {
        var merged = BgpSession.MergeAsPathWithAs4Path(
            [BgpConstants.AsPath.AsTrans, 65001u],
            [200000u]);

        Assert.Equal([200000u, 65001u], merged);
    }

    [Fact]
    public void MergeAsPathWithAs4Path_MismatchedLengths_Throws()
    {
        Assert.Throws<BgpParseException>(() => BgpSession.MergeAsPathWithAs4Path([65001u], [200000u]));
    }
}
