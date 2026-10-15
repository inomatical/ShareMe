using System.IO;
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

    public event EventHandler<InboxItem>? ItemReceived;

    public event EventHandler<IncomingTransferProgress>? ImageProgress;

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

            IFormCollection form;
            try
            {
                form = await request.ReadFormAsync(cancellationToken);
            }
            catch (BadHttpRequestException)
            {
                return Results.BadRequest("The multipart form is malformed.");
            }
            catch (InvalidDataException)
            {
                return Results.BadRequest("The multipart form is malformed.");
            }

            var kind = form["kind"].ToString();
            try
            {
                InboxItem? item;
                if (kind == "image" && form["content"].Count == 0 && form.Files.Count == 1 && form.Files[0].ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    var image = form.Files[0];
                    using var imageStream = image.OpenReadStream();
                    item = _storage.SaveImage(
                        image.FileName,
                        image.ContentType,
                        imageStream,
                        bytesReceived => ImageProgress?.Invoke(this, new IncomingTransferProgress(bytesReceived, image.Length)));
                }
                else if (kind == "file" && form["content"].Count == 0 && form.Files.Count == 1)
                {
                    var file = form.Files[0];
                    using var fileStream = file.OpenReadStream();
                    item = _storage.SaveFile(
                        file.FileName,
                        string.IsNullOrWhiteSpace(file.ContentType) ? null : file.ContentType,
                        fileStream);
                }
                else if (form["content"].Count == 1)
                {
                    var content = form["content"].ToString();
                    item = kind switch
                    {
                        "text" => _storage.SaveText(content),
                        "url" when Uri.TryCreate(content, UriKind.Absolute, out _) => _storage.SaveUrl(content),
                        _ => null,
                    };
                }
                else
                {
                    item = null;
                }

                if (item is null)
                {
                    return Results.BadRequest("A valid text, absolute URL, image, or single file is required.");
                }

                ItemReceived?.Invoke(this, item);
                return Results.Created($"/api/items/{item.Id}", item);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                return Results.Problem(
                    "The received item could not be persisted.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
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
            .Where(address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
            .Where(address => address.ToString().StartsWith("192.168."))
            .FirstOrDefault();

        return networkAddress ?? IPAddress.Loopback;
    }
}