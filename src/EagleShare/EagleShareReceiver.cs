using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace EagleShare;

/// <summary>
/// Hosts EagleShare's local HTTP endpoint while its main window is open.
/// Binding to all interfaces allows Windows to request private-network firewall
/// permission when the application first receives LAN traffic.
/// </summary>
public sealed class EagleShareReceiver : IAsyncDisposable
{
    public const int DefaultPort = 61432;

    private readonly EagleShareStorage _storage;
    private WebApplication? _application;

    public EagleShareReceiver(int port = DefaultPort)
        : this(EagleShareStorage.CreateDefault(), port)
    {
    }

    public EagleShareReceiver(EagleShareStorage storage, int port = DefaultPort)
    {
        if (port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
        {
            throw new ArgumentOutOfRangeException(nameof(port));
        }

        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        Port = port;
    }

    public int Port { get; }

    public bool IsRunning => _application is not null;

    public event EventHandler<InboxItem>? TextReceived;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_application is not null)
        {
            return;
        }

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseKestrel(options => options.ListenAnyIP(Port));

        var application = builder.Build();
        application.MapPost("/api/items", async (HttpRequest request, CancellationToken cancellationToken) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest("A multipart form is required.");
            }

            var form = await request.ReadFormAsync(cancellationToken);
            if (!string.Equals(form["kind"], "text", StringComparison.Ordinal) || form["content"].Count != 1)
            {
                return Results.BadRequest("A single text content field is required.");
            }

            var item = _storage.SaveText(form["content"].ToString());
            TextReceived?.Invoke(this, item);
            return Results.Created($"/api/items/{item.Id}", item);
        });
        application.MapFallback(() => Results.NotFound());

        try
        {
            await application.StartAsync(cancellationToken);
            _application = application;
        }
        catch
        {
            await application.DisposeAsync();
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        var application = Interlocked.Exchange(ref _application, null);
        if (application is null)
        {
            return;
        }

        await application.StopAsync(cancellationToken);
        await application.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    public static IPAddress GetLanAddress()
    {
        var networkAddress = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(networkInterface =>
                networkInterface.OperationalStatus == OperationalStatus.Up &&
                networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(networkInterface => networkInterface.GetIPProperties().UnicastAddresses)
            .Select(unicastAddress => unicastAddress.Address)
            .FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address));

        return networkAddress ?? IPAddress.Loopback;
    }
}