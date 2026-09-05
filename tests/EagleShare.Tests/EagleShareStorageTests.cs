using System.IO;
using System.Text.Json;
using Xunit;

namespace EagleShare.Tests;

public sealed class EagleShareStorageTests : IDisposable
{
    private readonly string _temporaryRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    [Fact]
    public void Initialize_CreatesEmptyInboxStorageAndMetadata()
    {
        var storage = new EagleShareStorage(_temporaryRoot);

        storage.Initialize();

        Assert.True(Directory.Exists(storage.RootPath));
        Assert.True(Directory.Exists(storage.IncomingPath));
        Assert.True(Directory.Exists(storage.InboxPath));
        Assert.True(File.Exists(storage.InboxMetadataPath));
        Assert.Equal("[]", File.ReadAllText(storage.InboxMetadataPath));
    }

    [Fact]
    public void Initialize_KeepsIncomingAndInboxContentSeparate()
    {
        var storage = new EagleShareStorage(_temporaryRoot);

        storage.Initialize();
        var partialUploadPath = Path.Combine(storage.IncomingPath, "partial-upload");
        File.WriteAllText(partialUploadPath, "incomplete");
        storage.Initialize();

        Assert.NotEqual(storage.IncomingPath, storage.InboxPath);
        Assert.True(File.Exists(partialUploadPath));
        Assert.Empty(Directory.EnumerateFiles(storage.InboxPath));
    }

    [Fact]
    public void SaveText_PersistsLiteralTextAndInboxMetadata()
    {
        var storage = new EagleShareStorage(_temporaryRoot);

        var item = storage.SaveText("Hello, \u4e16\u754c");

        Assert.Equal("text", item.Kind);
        Assert.Equal("Hello, \u4e16\u754c", File.ReadAllText(Path.Combine(storage.InboxPath, $"{item.Id}.txt")));
        var persistedItems = JsonSerializer.Deserialize<List<InboxItem>>(File.ReadAllText(storage.InboxMetadataPath));
        var persistedItem = Assert.Single(persistedItems!);
        Assert.Equal(item, persistedItem);
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryRoot))
        {
            Directory.Delete(_temporaryRoot, recursive: true);
        }
    }
}