using System.Buffers.Binary;
using BGPLite.Protocol;

namespace BGPLite.Tests;

public class BgpMessageTests
{
    [Fact]
    public void Keepalive_WriteThenRead_Roundtrip()
    {
        var buffer = new byte[64];
        var written = BgpMessageWriter.WriteMessage(BgpKeepaliveMessage.Instance, buffer);

        Assert.Equal(BgpConstants.MessageHeaderSize, written);

        var message = BgpMessageReader.ReadMessage(buffer.AsSpan(0, written));
        Assert.IsType<BgpKeepaliveMessage>(message);
    }

    [Fact]
    public void Open_WriteThenRead_Roundtrip()
    {
        var open = new BgpOpenMessage
        {
            Version = 4,
            Asn = 65444,
            HoldTime = 180,
            RouterId = 0x334B4214,
            Capabilities =
            [
                BgpCapabilityInfo.FourOctetAsn(65444),
                BgpCapabilityInfo.RouteRefresh(),
                BgpCapabilityInfo.MultiprotocolIpv4Unicast()
            ]
        };

        var buffer = new byte[512];
        var written = BgpMessageWriter.WriteMessage(open, buffer);

        var message = BgpMessageReader.ReadMessage(buffer.AsSpan(0, written));
        var readOpen = Assert.IsType<BgpOpenMessage>(message);

        Assert.Equal((byte)4, readOpen.Version);
        Assert.Equal((ushort)65444, readOpen.Asn);
        Assert.Equal((ushort)180, readOpen.HoldTime);
        Assert.Equal((uint)0x334B4214, readOpen.RouterId);
        Assert.Equal(3, readOpen.Capabilities.Count);
    }

    [Fact]
    public void Open_FourOctetAsn_CapabilityRoundtrip()
    {
        var asn = 200000u;
        var open = new BgpOpenMessage
        {
            Asn = 23456, // AS_TRANS
            HoldTime = 60,
            RouterId = 0x01020304,
            Capabilities = [BgpCapabilityInfo.FourOctetAsn(asn)]
        };

        var buffer = new byte[256];
        var written = BgpMessageWriter.WriteMessage(open, buffer);
        var readOpen = Assert.IsType<BgpOpenMessage>(BgpMessageReader.ReadMessage(buffer.AsSpan(0, written)));

        var effectiveAsn = CapabilityHelper.GetEffectiveAsn(readOpen);
        Assert.Equal(asn, effectiveAsn);
    }

    [Fact]
    public void Notification_WriteThenRead_Roundtrip()
    {
        var notif = new BgpNotificationMessage
        {
            ErrorCode = 2,
            SubErrorCode = 2
        };

        var buffer = new byte[64];
        var written = BgpMessageWriter.WriteMessage(notif, buffer);
        var readNotif = Assert.IsType<BgpNotificationMessage>(BgpMessageReader.ReadMessage(buffer.AsSpan(0, written)));

        Assert.Equal((byte)2, readNotif.ErrorCode);
        Assert.Equal((byte)2, readNotif.SubErrorCode);
        Assert.Null(readNotif.Data);
    }

    [Fact]
    public void Notification_WithData_Roundtrip()
    {
        var notif = new BgpNotificationMessage
        {
            ErrorCode = 2,
            SubErrorCode = 1,
            Data = [4]
        };

        var buffer = new byte[64];
        var written = BgpMessageWriter.WriteMessage(notif, buffer);
        var readNotif = Assert.IsType<BgpNotificationMessage>(BgpMessageReader.ReadMessage(buffer.AsSpan(0, written)));

        Assert.Equal((byte)2, readNotif.ErrorCode);
        Assert.Equal((byte)1, readNotif.SubErrorCode);
        Assert.Single(readNotif.Data!);
        Assert.Equal((byte)4, readNotif.Data[0]);
    }

    [Fact]
    public void Update_Empty_Roundtrip()
    {
        var update = new BgpUpdateMessage();

        var buffer = new byte[64];
        var written = BgpMessageWriter.WriteMessage(update, buffer);
        var readUpdate = Assert.IsType<BgpUpdateMessage>(BgpMessageReader.ReadMessage(buffer.AsSpan(0, written)));

        Assert.Empty(readUpdate.WithdrawnRoutes);
        Assert.Empty(readUpdate.PathAttributes);
        Assert.Empty(readUpdate.Nlri);
    }

    [Fact]
    public void Update_WithRoutesAndAttributes_Roundtrip()
    {
        var update = new BgpUpdateMessage
        {
            PathAttributes =
            [
                AttributeHelper.WriteOrigin(BgpOrigin.Igp),
                AttributeHelper.WriteAsPath([65444u, 65001u], fourByteAsn: true),
                AttributeHelper.WriteNextHop(0xC0A80101),
                AttributeHelper.WriteCommunities([0x0000FF01])
            ],
            Nlri =
            [
                new IpPrefix(0xC0A80000, 24), // 192.168.0.0/24
                new IpPrefix(0x0A000000, 8)   // 10.0.0.0/8
            ]
        };

        var buffer = new byte[512];
        var written = BgpMessageWriter.WriteMessage(update, buffer);
        var readUpdate = Assert.IsType<BgpUpdateMessage>(BgpMessageReader.ReadMessage(buffer.AsSpan(0, written)));

        Assert.Equal(2, readUpdate.Nlri.Count);
        Assert.Equal(0xC0A80000u, readUpdate.Nlri[0].Address);
        Assert.Equal((byte)24, readUpdate.Nlri[0].Length);
        Assert.Equal(0x0A000000u, readUpdate.Nlri[1].Address);
        Assert.Equal((byte)8, readUpdate.Nlri[1].Length);

        Assert.Equal(4, readUpdate.PathAttributes.Count);

        // Verify AS_PATH with 4-byte ASN
        var asPathAttr = readUpdate.PathAttributes.First(a => a.TypeCode == BgpConstants.Attribute.AsPath);
        var ases = AttributeHelper.ReadAsPath(asPathAttr, fourByteAsn: true);
        Assert.Equal([65444u, 65001u], ases);

        // Verify NEXT_HOP
        var nextHopAttr = readUpdate.PathAttributes.First(a => a.TypeCode == BgpConstants.Attribute.NextHop);
        Assert.Equal(0xC0A80101u, AttributeHelper.ReadNextHop(nextHopAttr));

        // Verify COMMUNITY
        var commAttr = readUpdate.PathAttributes.First(a => a.TypeCode == BgpConstants.Attribute.Community);
        var communities = AttributeHelper.ReadCommunities(commAttr);
        Assert.Single(communities);
        Assert.Equal(0x0000FF01u, communities[0]);
    }

    [Fact]
    public void Update_WithWithdrawals_Roundtrip()
    {
        var update = new BgpUpdateMessage
        {
            WithdrawnRoutes = [new IpPrefix(0xC0A80000, 24)]
        };

        var buffer = new byte[256];
        var written = BgpMessageWriter.WriteMessage(update, buffer);
        var readUpdate = Assert.IsType<BgpUpdateMessage>(BgpMessageReader.ReadMessage(buffer.AsSpan(0, written)));

        Assert.Single(readUpdate.WithdrawnRoutes);
        Assert.Equal(0xC0A80000u, readUpdate.WithdrawnRoutes[0].Address);
        Assert.Equal((byte)24, readUpdate.WithdrawnRoutes[0].Length);
    }

    [Fact]
    public void Marker_IsAllOnes()
    {
        for (var i = 0; i < 16; i++)
            Assert.Equal(0xFF, BgpConstants.Marker[i]);
    }

    [Fact]
    public void GetBufferSize_MatchesWriteSize()
    {
        var open = new BgpOpenMessage
        {
            Asn = 100,
            HoldTime = 60,
            RouterId = 0x01020304,
            Capabilities = [BgpCapabilityInfo.FourOctetAsn(100)]
        };

        var expectedSize = BgpMessageWriter.GetBufferSize(open);
        var buffer = new byte[expectedSize];
        var actualSize = BgpMessageWriter.WriteMessage(open, buffer);

        Assert.Equal(expectedSize, actualSize);
    }

    [Fact]
    public void ReadMessage_InvalidMarker_Throws()
    {
        var buffer = new byte[19];
        // All zeros — invalid marker
        Assert.Throws<BgpParseException>(() => BgpMessageReader.ReadMessage(buffer));
    }

    [Fact]
    public void ReadMessage_TooShort_Throws()
    {
        var buffer = new byte[10];
        Assert.Throws<BgpParseException>(() => BgpMessageReader.ReadMessage(buffer));
    }

    [Fact]
    public void RouteRefresh_RoundTrip()
    {
        var msg = new BgpRouteRefreshMessage
        {
            Afi = BgpConstants.Afi.IPv4,
            Reserved = 0,
            Safi = BgpConstants.Safi.Unicast
        };

        var buffer = new byte[64];
        var written = BgpMessageWriter.WriteMessage(msg, buffer);
        var read = BgpMessageReader.ReadMessage(buffer.AsSpan(0, written));

        var rr = Assert.IsType<BgpRouteRefreshMessage>(read);
        Assert.Equal(BgpConstants.Afi.IPv4, rr.Afi);
        Assert.Equal((byte)0, rr.Reserved);
        Assert.Equal(BgpConstants.Safi.Unicast, rr.Safi);
    }

    [Fact]
    public void RouteRefresh_InvalidLength_Throws()
    {
        var buffer = new byte[64];
        BgpConstants.Marker.CopyTo(buffer);
        BinaryPrimitives.WriteUInt16BigEndian(buffer[16..], (ushort)(BgpConstants.MessageHeaderSize + 3));
        buffer[18] = (byte)BgpMessageType.RouteRefresh;
        buffer[19] = 0;
        buffer[20] = 1;
        buffer[21] = 1;

        Assert.Throws<BgpParseException>(() => BgpMessageReader.ReadMessage(buffer.AsSpan(0, 22)));
    }
}
