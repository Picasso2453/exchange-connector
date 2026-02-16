using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using Xws.Data.Output;
using Xws.Data.Subscriptions;
using Xws.Data.WebSocket;

namespace xws.tests;

public sealed class WebSocketRunnerStaleTests
{
    [Fact]
    public async Task Runner_ReconnectsOnStaleConnection()
    {
        var port = GetFreePort();
        using var cts = new CancellationTokenSource();
        Task serverTask;
        try
        {
            serverTask = RunServerAsync(port, cts.Token);
        }
        catch (HttpListenerException)
        {
            return;
        }

        var registry = new SubscriptionRegistry();
        registry.Add(new SubscriptionRequest(new SubscriptionKey("test", "params"), "{\"op\":\"sub\"}"));
        var runner = new WebSocketRunner(new JsonlWriter(_ => { }), registry);
        var options = new WebSocketRunnerOptions
        {
            StaleTimeout = TimeSpan.FromMilliseconds(250),
            Timeout = TimeSpan.FromSeconds(4)
        };

        var exitCode = await runner.RunAsync(new Uri($"ws://localhost:{port}/ws/"), options, CancellationToken.None);

        cts.Cancel();
        await serverTask;

        Assert.Equal(1, exitCode);
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task RunServerAsync(int port, CancellationToken cancellationToken)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/ws/");
        listener.Start();
        using var cancelRegistration = cancellationToken.Register(listener.Stop);

        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync();
            }
            catch (HttpListenerException)
            {
                break;
            }

            if (!context.Request.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                continue;
            }

            var wsContext = await context.AcceptWebSocketAsync(subProtocol: null);
            _ = Task.Run(async () =>
            {
                var socket = wsContext.WebSocket;
                var buffer = new byte[1024];
                try
                {
                    await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    if (socket.State == WebSocketState.Open)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", cancellationToken);
                    }
                }
                catch
                {
                }
            }, cancellationToken);
        }
    }
}
