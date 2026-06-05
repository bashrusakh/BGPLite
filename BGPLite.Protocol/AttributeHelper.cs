using System.Buffers.Binary;
using System.Net;

namespace BGPLite.Protocol;

public static class AttributeHelper
{
    public static BgpOrigin ReadOrigin(PathAttribute attr)
    {
        return (BgpOrigin)attr.Data[0];
    }

    public static PathAttribute WriteOrigin(BgpOrigin origin)
    {
        return new PathAttribute
        {
            Flags = BgpConstants.Attribute.FlagTransitive,
            TypeCode = BgpConstants.Attribute.Origin,
            Data = [(byte)origin]
        };
    }

    public static uint[] ReadAsPath(PathAttribute attr, bool fourByteAsn)
    {
        var ases = new List<uint>();
        var offset = 0;
        while (offset < attr.Data.Length)
        {
            var segmentType = attr.Data[offset++];
            var segmentLength = attr.Data[offset++];

            for (var i = 0; i < segmentLength; i++)
            {
                if (fourByteAsn && offset + 4 <= attr.Data.Length)
                {
                    ases.Add(BinaryPrimitives.ReadUInt32BigEndian(attr.Data.AsSpan(offset)));
                    offset += 4;
                }
                else if (offset + 2 <= attr.Data.Length)
                {
                    ases.Add(BinaryPrimitives.ReadUInt16BigEndian(attr.Data.AsSpan(offset)));
                    offset += 2;
                }
            }
        }
        return ases.ToArray();
    }

    public static PathAttribute WriteAsPath(uint[] ases, bool fourByteAsn)
    {
        var asSize = fourByteAsn ? 4 : 2;
        var data = new byte[2 + ases.Length * asSize];
        data[0] = BgpConstants.AsPath.AsSequence;
        data[1] = (byte)ases.Length;

        var offset = 2;
        foreach (var asn in ases)
        {
            if (fourByteAsn)
            {
                BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(offset), asn);
                offset += 4;
            }
            else
            {
                BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(offset), (ushort)asn);
                offset += 2;
            }
        }

        return new PathAttribute
        {
            Flags = BgpConstants.Attribute.FlagTransitive,
            TypeCode = BgpConstants.Attribute.AsPath,
            Data = data
        };
    }

    public static uint ReadNextHop(PathAttribute attr)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(attr.Data);
    }

    public static PathAttribute WriteNextHop(uint nextHop)
    {
        var data = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(data, nextHop);
        return new PathAttribute
        {
            Flags = BgpConstants.Attribute.FlagTransitive,
            TypeCode = BgpConstants.Attribute.NextHop,
            Data = data
        };
    }

    public static uint[] ReadCommunities(PathAttribute attr)
    {
        var count = attr.Data.Length / 4;
        var communities = new uint[count];
        for (var i = 0; i < count; i++)
            communities[i] = BinaryPrimitives.ReadUInt32BigEndian(attr.Data.AsSpan(i * 4));
        return communities;
    }

    public static PathAttribute WriteCommunities(uint[] communities)
    {
        var data = new byte[communities.Length * 4];
        for (var i = 0; i < communities.Length; i++)
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(i * 4), communities[i]);
        return new PathAttribute
        {
            Flags = BgpConstants.Attribute.FlagOptional | BgpConstants.Attribute.FlagTransitive,
            TypeCode = BgpConstants.Attribute.Community,
            Data = data
        };
    }

    public static bool IsKnownAttribute(byte typeCode) => typeCode switch
    {
        BgpConstants.Attribute.Origin => true,
        BgpConstants.Attribute.AsPath => true,
        BgpConstants.Attribute.NextHop => true,
        BgpConstants.Attribute.Community => true,
        BgpConstants.Attribute.Med => true,
        BgpConstants.Attribute.LocalPref => true,
        BgpConstants.Attribute.AtomicAggregate => true,
        BgpConstants.Attribute.Aggregator => true,
        _ => false
    };
}
