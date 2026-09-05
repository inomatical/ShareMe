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
    public async Task MalformedMultipartPost_ReturnsClearClientError()
    {
        await using var harness = await EagleShareReceiverContractHarness.StartAsync();

        var response = await harness.PostMalformedFormAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("malformed", response.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MultipartPost_WhenStorageIsUnavailable_ReturnsClearServerError()
    {
        var storageRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        File.WriteAllText(storageRootPath, "not a directory");
        var receiver = new EagleShareReceiver(new EagleShareStorage(storageRootPath), GetAvailablePort());
        await receiver.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{receiver.Port}/") };
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("text", Encoding.UTF8), "kind");
        form.Add(new StringContent("Could not be stored", Encoding.UTF8), "content");

        try
        {
            using var response = await client.PostAsync("api/items", form);
            var responseBody = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Contains("could not be persisted", responseBody, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await receiver.DisposeAsync();
            File.Delete(storageRootPath);
        }
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

    [Fact]
    public async Task MultipartPost_WithValidUrl_ReturnsSuccessAfterUrlIsDurable()
    {
        await using var harness = await EagleShareReceiverContractHarness.StartAsync();

        var response = await harness.PostItemAsync("url", "https://example.com/path?query=value#section");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = JsonSerializer.Deserialize<InboxItem>(response.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(item);
        Assert.Equal("url", item.Kind);
        Assert.Equal(item.Content, File.ReadAllText(Path.Combine(harness.Storage.InboxPath, $"{item.Id}.url")));
        var persistedItems = JsonSerializer.Deserialize<List<InboxItem>>(File.ReadAllText(harness.Storage.InboxMetadataPath));
        Assert.Contains(item, persistedItems!);
    }

    [Fact]
    public async Task MultipartPost_WithImage_ReturnsSuccessAfterImageIsDurable()
    {
        await using var harness = await EagleShareReceiverContractHarness.StartAsync();
        var imageBytes = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };

        var response = await harness.PostImageAsync("holiday.png", "image/png", imageBytes);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = JsonSerializer.Deserialize<InboxItem>(response.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(item);
        Assert.Equal("image", item.Kind);
        Assert.Equal("holiday.png", item.OriginalFileName);
        Assert.Equal("image/png", item.MimeType);
        Assert.Equal(imageBytes, File.ReadAllBytes(Path.Combine(harness.Storage.InboxPath, item.StoredFileName!)));
        Assert.Empty(Directory.EnumerateFiles(harness.Storage.IncomingPath));
    }

    [Fact]
    public async Task MultipartPost_WithFile_ReturnsSuccessAfterFileIsDurableWithMetadata()
    {
        await using var harness = await EagleShareReceiverContractHarness.StartAsync();
        var fileBytes = new byte[] { 1, 2, 3, 4 };

        var response = await harness.PostFileAsync("report.pdf", "application/pdf", fileBytes);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = JsonSerializer.Deserialize<InboxItem>(response.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(item);
        Assert.Equal("file", item.Kind);
        Assert.Equal("report.pdf", item.OriginalFileName);
        Assert.Equal("application/pdf", item.MimeType);
        Assert.Equal(fileBytes.Length, item.Size);
        Assert.Equal(Path.Combine(harness.Storage.InboxPath, item.StoredFileName!), item.LocalPath);
        Assert.Equal(fileBytes, File.ReadAllBytes(item.LocalPath!));
        Assert.Empty(Directory.EnumerateFiles(harness.Storage.IncomingPath));
    }

    [Fact]
    public async Task Receiver_AfterStopping_RejectsNewRequests()
    {
        var receiver = new EagleShareReceiver(GetAvailablePort());
        await receiver.StartAsync();
        await receiver.StopAsync();
        using var client = new HttpClient();

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync($"http://127.0.0.1:{receiver.Port}/"));
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, port: 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
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

    public async Task<TransferHttpResponse> PostMalformedFormAsync()
    {
        using var content = new StringContent("this is not a multipart form", Encoding.UTF8, "multipart/form-data");
        using var response = await _client.PostAsync("api/items", content);
        return new TransferHttpResponse(response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    public async Task<TransferHttpResponse> PostImageAsync(string fileName, string mimeType, byte[] content)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("image", Encoding.UTF8), "kind");
        var image = new ByteArrayContent(content);
        image.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
        form.Add(image, "file", fileName);

        using var response = await _client.PostAsync("api/items", form);
        return new TransferHttpResponse(response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    public async Task<TransferHttpResponse> PostFileAsync(string fileName, string mimeType, byte[] content)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("file", Encoding.UTF8), "kind");
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
        form.Add(file, "file", fileName);

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