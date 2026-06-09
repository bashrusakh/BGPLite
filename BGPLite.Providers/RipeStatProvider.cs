using System.Net;
using System.Text.Json;
using BGPLite.Protocol;
using Microsoft.Extensions.Logging;

namespace BGPLite.Providers;

public sealed class RipeStatProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<RipeStatProvider> _logger;

    public RipeStatProvider(HttpClient http, ILogger<RipeStatProvider> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<(uint Prefix, byte PrefixLength)>> GetPrefixesAsync(uint asn, CancellationToken ct = default)
    {
        var url = $"https://stat.ripe.net/data/ris-prefixes/data.json?resource=AS{asn}&list_prefixes=true";
        using var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);

        var prefixes = doc.RootElement
            .GetProperty("data")
            .GetProperty("prefixes")
            .GetProperty("v4")
            .GetProperty("originating");

        var result = new List<(uint Prefix, byte PrefixLength)>(prefixes.GetArrayLength());

        foreach (var element in prefixes.EnumerateArray())
        {
            var cidr = element.GetString()!;
            var slash = cidr.IndexOf('/');
            var ip = IPAddress.Parse(cidr[..slash]);
            var length = byte.Parse(cidr[(slash + 1)..]);
            var prefix = BgpConstants.IPAddressToUint(ip);
            result.Add((prefix, length));
        }

        _logger.LogInformation("AS{Asn}: fetched {Count} prefixes", asn, result.Count);
        return result;
    }
}
