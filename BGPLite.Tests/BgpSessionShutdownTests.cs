using System.Net;
using System.Net.Sockets;
using BGPLite.Configuration;
using BGPLite.Protocol;
using BGPLite.Routing;
using BGPLite.Server;
using Microsoft.Extensions.Logging;

namespace BGPLite.Tests;

public class BgpSessionShutdownTests
{
    private static void ReadExact(Socket s, byte[] buf, int offset, int count)
    {
        var got = 0;
        while (got < count)
        {
            var n = s.Receive(buf, offset + got, count - got, SocketFlags.None);
            if (n == 0) throw new IOException("socket closed");
            got += n;
        }
    }

    private static (Socket server, Socket client) ConnectedPair()
    {
        using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        client.Connect(listener.LocalEndPoint!);
        return (listener.Accept(), client);
    }

    private static BgpSession NewSession(Socket server) => new(
        server,
        new PeerConfig { Address = "127.0.0.1" },
        new BgpConfig { Asn = 65001, RouterId = "127.0.0.1" },
        new RouteTable(),
        AllowAllFilter.Instance,
        new BgpMetrics(),
        new NopLogger<BgpSession>());

    private sealed class NopLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NopDisposable.Instance;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
        private sealed class NopDisposable : IDisposable
        {
            public static readonly NopDisposable Instance = new();
            public void Dispose() { }
        }
    }

    [Fact]
    public async Task NotifyCeaseAsync_Writes_Cease_Notification_To_Wire()
    {
        var (server, client) = ConnectedPair();
        using var clientSock = client;
        using var session = NewSession(server);

        await session.NotifyCeaseAsync();

        var buf = new byte[64];
        ReadExact(client, buf, 0, BgpConstants.MessageHeaderSize);
        var len = BgpMessageReader.GetMessageLength(buf);
        ReadExact(client, buf, BgpConstants.MessageHeaderSize, len - BgpConstants.MessageHeaderSize);

        var msg = BgpMessageReader.ReadMessage(buf.AsSpan(0, len));
        var notif = Assert.IsType<BgpNotificationMessage>(msg);
        Assert.Equal(BgpConstants.Error.Cease, notif.ErrorCode);
        Assert.Equal(BgpConstants.SubError.Unspecific, notif.SubErrorCode);
    }

    [Fact]
    public async Task NotifyCeaseAsync_Swallows_Error_When_Socket_Closed()
    {
        // If the socket is already gone, NotifyCeaseAsync must not throw (best-effort on shutdown).
        var (server, client) = ConnectedPair();
        using var clientSock = client;
        using var session = NewSession(server);

        server.Close(); // kill the server side before notifying

        // Should complete without throwing despite the closed socket.
        await session.NotifyCeaseAsync();
    }
}
