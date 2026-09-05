using System.IO;
using System.Collections.Generic;
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

    [Fact]
    public void SaveUrl_PersistsDistinctUrlContentAndInboxMetadata()
    {
        var storage = new EagleShareStorage(_temporaryRoot);

        var item = storage.SaveUrl("https://example.com/path?query=value#section");

        Assert.Equal("url", item.Kind);
        Assert.Equal(item.Content, File.ReadAllText(Path.Combine(storage.InboxPath, $"{item.Id}.url")));
        var persistedItems = JsonSerializer.Deserialize<List<InboxItem>>(File.ReadAllText(storage.InboxMetadataPath));
        Assert.Equal(item, Assert.Single(persistedItems!));
    }

    [Fact]
    public void SaveImage_StreamsToIncomingThenFinalizesCollisionSafeImageMetadata()
    {
        var storage = new EagleShareStorage(_temporaryRoot);
        var imageBytes = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        var progress = new List<long>();

        using var firstImage = new MemoryStream(imageBytes);
        using var secondImage = new MemoryStream(imageBytes);
        var firstItem = storage.SaveImage("holiday.png", "image/png", firstImage, progress.Add);
        var secondItem = storage.SaveImage("holiday.png", "image/png", secondImage);

        Assert.Equal("image", firstItem.Kind);
        Assert.Equal("holiday.png", firstItem.OriginalFileName);
        Assert.Equal("image/png", firstItem.MimeType);
        Assert.NotEqual(firstItem.StoredFileName, secondItem.StoredFileName);
        Assert.Equal(imageBytes, File.ReadAllBytes(Path.Combine(storage.InboxPath, firstItem.StoredFileName!)));
        Assert.Empty(Directory.EnumerateFiles(storage.IncomingPath));
        Assert.Equal(imageBytes.Length, Assert.Single(progress));
        var persistedItems = JsonSerializer.Deserialize<List<InboxItem>>(File.ReadAllText(storage.InboxMetadataPath));
        Assert.Equal(2, persistedItems!.Count);
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryRoot))
        {
            Directory.Delete(_temporaryRoot, recursive: true);
        }
    }
}