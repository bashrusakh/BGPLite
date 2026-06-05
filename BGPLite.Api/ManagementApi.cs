using System.Net;
using System.Text;
using System.Text.Json;
using BGPLite.Routing;
using BGPLite.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BGPLite.Api;

public sealed class ManagementApi : IHostedService, IDisposable
{
    private readonly PeerStore _store;
    private readonly RouteTable _routeTable;
    private readonly ILogger<ManagementApi> _logger;
    private HttpListener? _listener;
    private Task? _listenTask;
    private CancellationTokenSource _cts = new();

    public ManagementApi(
        PeerStore store,
        RouteTable routeTable,
        ILogger<ManagementApi> logger)
    {
        _store = store;
        _routeTable = routeTable;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://+:5000/");
        _listener.Start();

        _logger.LogInformation("Management API listening on http://+:5000/");
        _listenTask = ListenAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        _listener?.Stop();
        if (_listenTask is not null)
        {
            try { await _listenTask; } catch { }
        }
    }

    private async Task ListenAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var ctx = await _listener!.GetContextAsync();
                if (ct.IsCancellationRequested) break;
                _ = HandleAsync(ctx);
            }
            catch (HttpListenerException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Management API error");
            }
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx)
    {
        var path = ctx.Request.Url!.AbsolutePath;
        var method = ctx.Request.HttpMethod;

        try
        {
            ApiResponse response;

            if (method == "GET" && path == "/api/peers")
                response = HandleGetPeers();
            else if (method == "GET" && path == "/api/routes/count")
                response = HandleGetRouteCount();
            else if (method == "GET" && path.StartsWith("/api/peer/") && path.EndsWith("/communities"))
                response = HandleGetPeerCommunities(ExtractPeerIp(path));
            else if (method == "PUT" && path.StartsWith("/api/peer/") && path.EndsWith("/communities"))
                response = await HandleSetPeerCommunities(ExtractPeerIp(path), ctx);
            else if (method == "DELETE" && path.StartsWith("/api/peer/") && path.EndsWith("/communities"))
                response = HandleDeletePeerCommunities(ExtractPeerIp(path));
            else if (method == "PUT" && path.StartsWith("/api/peer/") && path.EndsWith("/description"))
                response = await HandleSetPeerDescription(ExtractPeerIp(path), ctx);
            else
                response = ApiResponse.Error("Not found", 404);

            await WriteResponse(ctx, response);
        }
        catch (Exception ex)
        {
            await WriteResponse(ctx, ApiResponse.Error(ex.Message, 500));
        }
    }

    private static string ExtractPeerIp(string path)
    {
        // /api/peer/{ip}/communities → segments: ["", "api", "peer", "{ip}", ...]
        return path.Split('/')[3];
    }

    private ApiResponse HandleGetPeers()
    {
        var peers = _store.GetAllPeers();
        return ApiResponse.Ok(peers.Select(p => new
        {
            ip = p.Ip,
            asn = p.Asn,
            description = p.Description,
            connectedAt = p.ConnectedAt,
            communities = p.Communities.Select(CommunityToString),
            allRoutes = p.Communities.Count == 0
        }));
    }

    private ApiResponse HandleGetPeerCommunities(string peerIp)
    {
        var communities = _store.GetCommunities(peerIp);
        return ApiResponse.Ok(new
        {
            ip = peerIp,
            communities = communities.Select(CommunityToString),
            allRoutes = communities.Count == 0
        });
    }

    private async Task<ApiResponse> HandleSetPeerCommunities(string peerIp, HttpListenerContext ctx)
    {
        using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        var data = JsonSerializer.Deserialize<SetCommunitiesRequest>(body);

        if (data?.Communities is null)
            return ApiResponse.Error("Invalid request body", 400);

        var communities = new HashSet<uint>();
        foreach (var c in data.Communities)
            communities.Add(ParseCommunity(c));

        _store.SetCommunities(peerIp, communities);

        _logger.LogInformation("Updated communities for {Peer}: {Communities}",
            peerIp, string.Join(", ", communities.Select(CommunityToString)));

        return ApiResponse.Ok(new { ip = peerIp, communities = communities.Select(CommunityToString) });
    }

    private async Task<ApiResponse> HandleSetPeerDescription(string peerIp, HttpListenerContext ctx)
    {
        using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        var data = JsonSerializer.Deserialize<SetDescriptionRequest>(body);

        if (data?.Description is null)
            return ApiResponse.Error("Invalid request body", 400);

        _store.SetDescription(peerIp, data.Description);

        _logger.LogInformation("Updated description for {Peer}: {Desc}", peerIp, data.Description);

        return ApiResponse.Ok(new { ip = peerIp, description = data.Description });
    }

    private ApiResponse HandleDeletePeerCommunities(string peerIp)
    {
        _store.ClearCommunities(peerIp);

        _logger.LogInformation("Removed community filter for {Peer}", peerIp);
        return ApiResponse.Ok(new { ip = peerIp, allRoutes = true });
    }

    private ApiResponse HandleGetRouteCount()
    {
        var routes = _routeTable.GetAll();
        var byCommunity = routes
            .SelectMany(r => r.Communities.Length == 0
                ? [(community: 0u, route: r)]
                : r.Communities.Select(c => (community: c, route: r)))
            .GroupBy(x => x.community)
            .ToDictionary(g => g.Key == 0 ? "default" : CommunityToString(g.Key), g => g.Count());

        return ApiResponse.Ok(new { total = routes.Count, byCommunity });
    }

    private static uint ParseCommunity(string community)
    {
        var colon = community.IndexOf(':');
        var asn = uint.Parse(community[..colon]);
        var value = uint.Parse(community[(colon + 1)..]);
        return (asn << 16) | (value & 0xFFFF);
    }

    private static string CommunityToString(uint community)
    {
        var asn = community >> 16;
        var value = community & 0xFFFF;
        return $"{asn}:{value}";
    }

    private static async Task WriteResponse(HttpListenerContext ctx, ApiResponse response)
    {
        ctx.Response.StatusCode = response.StatusCode;
        ctx.Response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(response.Body);
        var bytes = Encoding.UTF8.GetBytes(json);
        await ctx.Response.OutputStream.WriteAsync(bytes);
        ctx.Response.Close();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _listener?.Close();
    }

    private record SetCommunitiesRequest(List<string> Communities);
    private record SetDescriptionRequest(string Description);

    private record ApiResponse(object? Body, int StatusCode = 200)
    {
        public static ApiResponse Ok(object data) => new(data);
        public static ApiResponse Error(string message, int code) => new(new { error = message }, code);
    }
}
