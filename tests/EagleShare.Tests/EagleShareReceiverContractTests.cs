using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Xunit;

namespace EagleShare.Tests;

public sealed class EagleShareReceiverContractTests
{
    [Fact]
    public async Task MultipartPost_ToItemsEndpoint_ExposesThePublicHttpResponse()
    {
        await using var harness = await EagleShareReceiverContractHarness.StartAsync();

        var response = await harness.PostItemAsync("text", "Hello from BearShare");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.False(response.IsClientError);
        Assert.False(response.IsServerError);
    }

    [Fact]
    public async Task MultipartPost_WithUnsupportedKind_ReturnsClientError()
    {
        await using var harness = await EagleShareReceiverContractHarness.StartAsync();

        var response = await harness.PostItemAsync("file", "not a text transfer");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(response.IsClientError);
        Assert.False(response.IsServerError);
    }

    [Fact]
    public async Task MultipartPost_ToItemsEndpoint_ReturnsSuccessAfterTextIsDurable()
    {
        await using var harness = await EagleShareReceiverContractHarness.StartAsync();

        var response = await harness.PostItemAsync("text", "Hello, \u4e16\u754c");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = JsonSerializer.Deserialize<InboxItem>(response.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(item);
        Assert.Equal("Hello, \u4e16\u754c", File.ReadAllText(Path.Combine(harness.Storage.InboxPath, $"{item.Id}.txt")));
        var persistedItems = JsonSerializer.Deserialize<List<InboxItem>>(File.ReadAllText(harness.Storage.InboxMetadataPath));
        Assert.Contains(item, persistedItems!);
    }
}

internal sealed class EagleShareReceiverContractHarness : IAsyncDisposable
{
    private readonly EagleShareReceiver _receiver;
    private readonly HttpClient _client;
    private readonly string _temporaryRoot;

    private EagleShareReceiverContractHarness(EagleShareReceiver receiver, EagleShareStorage storage, string temporaryRoot)
    {
        _receiver = receiver;
        Storage = storage;
        _temporaryRoot = temporaryRoot;
        _client = new HttpClient
        {
            BaseAddress = new Uri($"http://127.0.0.1:{receiver.Port}/")
        };
    }

    public EagleShareStorage Storage { get; }

    public static async Task<EagleShareReceiverContractHarness> StartAsync()
    {
        var temporaryRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var storage = new EagleShareStorage(temporaryRoot);
        var receiver = new EagleShareReceiver(storage, GetAvailablePort());
        var harness = new EagleShareReceiverContractHarness(receiver, storage, temporaryRoot);

        try
        {
            await receiver.StartAsync();
            return harness;
        }
        catch
        {
            await harness.DisposeAsync();
            throw;
        }
    }

    public async Task<TransferHttpResponse> PostItemAsync(string kind, string content)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(kind, Encoding.UTF8), "kind");
        form.Add(new StringContent(content, Encoding.UTF8), "content");

        using var response = await _client.PostAsync("api/items", form);
        return new TransferHttpResponse(response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _receiver.DisposeAsync();
        if (Directory.Exists(_temporaryRoot))
        {
            Directory.Delete(_temporaryRoot, recursive: true);
        }
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, port: 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}

internal sealed record TransferHttpResponse(HttpStatusCode StatusCode, string Body)
{
    public bool IsClientError => (int)StatusCode is >= 400 and < 500;

    public bool IsServerError => (int)StatusCode is >= 500 and < 600;
}