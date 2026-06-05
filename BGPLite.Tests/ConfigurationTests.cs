using BGPLite.Configuration;

namespace BGPLite.Tests;

public class ConfigurationTests
{
    [Fact]
    public void LoadFromText_ParsesValidYaml()
    {
        var yaml = """
            Bgp:
              Asn: 65444
              RouterId: 10.0.0.1
              KeepAlive: 60
              HoldTime: 180

            Peers:
              - Address: 10.0.0.2
                RemoteAsn: 65001
                Description: "upstream"
            """;

        var config = ConfigLoader.LoadFromText(yaml);

        Assert.Equal(65444u, config.Bgp.Asn);
        Assert.Equal("10.0.0.1", config.Bgp.RouterId);
        Assert.Equal(60, config.Bgp.KeepAlive);
        Assert.Equal(180, config.Bgp.HoldTime);

        Assert.Single(config.Peers);
        Assert.Equal("10.0.0.2", config.Peers[0].Address);
        Assert.Equal(65001u, config.Peers[0].RemoteAsn);
        Assert.Equal("upstream", config.Peers[0].Description);
    }

    [Fact]
    public void LoadFromText_MultiplePeers()
    {
        var yaml = """
            Bgp:
              Asn: 65444
              RouterId: 10.0.0.1

            Peers:
              - Address: 10.0.0.2
                RemoteAsn: 65001
              - Address: 10.0.0.3
                RemoteAsn: 65002
            """;

        var config = ConfigLoader.LoadFromText(yaml);
        Assert.Equal(2, config.Peers.Count);
    }

    [Fact]
    public void LoadFromText_DefaultValues()
    {
        var yaml = """
            Bgp:
              Asn: 65444
              RouterId: 10.0.0.1
            """;

        var config = ConfigLoader.LoadFromText(yaml);

        Assert.Equal(60, config.Bgp.KeepAlive);
        Assert.Equal(180, config.Bgp.HoldTime);
        Assert.Empty(config.Peers);
    }
}
