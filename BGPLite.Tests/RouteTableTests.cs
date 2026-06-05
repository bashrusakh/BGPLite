using BGPLite.Routing;

namespace BGPLite.Tests;

public class RouteTableTests
{
    [Fact]
    public void AddOrUpdate_NewRoute_ReturnsTrue()
    {
        var table = new RouteTable();
        var route = new Route { Prefix = 0xC0A80000, PrefixLength = 24, NextHop = 0x01020304 };

        Assert.True(table.AddOrUpdate(route));
        Assert.Equal(1, table.Count);
    }

    [Fact]
    public void AddOrUpdate_ExistingRoute_ReturnsFalse()
    {
        var table = new RouteTable();
        var route1 = new Route { Prefix = 0xC0A80000, PrefixLength = 24, NextHop = 0x01020304 };
        var route2 = new Route { Prefix = 0xC0A80000, PrefixLength = 24, NextHop = 0x05060708 };

        table.AddOrUpdate(route1);
        Assert.False(table.AddOrUpdate(route2));
        Assert.Equal(1, table.Count);

        var stored = table.Get(0xC0A80000, 24);
        Assert.Equal(0x05060708u, stored!.NextHop);
    }

    [Fact]
    public void Remove_ExistingRoute_ReturnsTrue()
    {
        var table = new RouteTable();
        table.AddOrUpdate(new Route { Prefix = 0xC0A80000, PrefixLength = 24, NextHop = 0x01020304 });

        Assert.True(table.Remove(0xC0A80000, 24));
        Assert.Equal(0, table.Count);
    }

    [Fact]
    public void Remove_NonExistingRoute_ReturnsFalse()
    {
        var table = new RouteTable();
        Assert.False(table.Remove(0xC0A80000, 24));
    }

    [Fact]
    public void GetAll_ReturnsAllRoutes()
    {
        var table = new RouteTable();
        table.AddOrUpdate(new Route { Prefix = 0xC0A80000, PrefixLength = 24, NextHop = 0x01020304 });
        table.AddOrUpdate(new Route { Prefix = 0x0A000000, PrefixLength = 8, NextHop = 0x05060708 });

        var routes = table.GetAll();
        Assert.Equal(2, routes.Count);
    }

    [Fact]
    public void Clear_RemovesAllRoutes()
    {
        var table = new RouteTable();
        table.AddOrUpdate(new Route { Prefix = 0xC0A80000, PrefixLength = 24, NextHop = 0x01020304 });
        table.Clear();
        Assert.Equal(0, table.Count);
    }
}
