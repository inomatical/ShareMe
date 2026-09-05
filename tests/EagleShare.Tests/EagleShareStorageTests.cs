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

    [Fact]
    public void CopyImageTo_CreatesCopyWithoutRemovingInboxImage()
    {
        var storage = new EagleShareStorage(_temporaryRoot);
        var imageBytes = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        var destinationPath = Path.Combine(_temporaryRoot, "saved-image.png");

        using var image = new MemoryStream(imageBytes);
        var item = storage.SaveImage("holiday.png", "image/png", image);

        storage.CopyImageTo(item, destinationPath);

        Assert.Equal(imageBytes, File.ReadAllBytes(destinationPath));
        Assert.Equal(imageBytes, File.ReadAllBytes(Path.Combine(storage.InboxPath, item.StoredFileName!)));
    }

    [Fact]
    public void SaveFile_StreamsToInboxAndRecordsFileMetadata()
    {
        var storage = new EagleShareStorage(_temporaryRoot);
        var fileBytes = new byte[] { 1, 2, 3, 4 };

        using var file = new MemoryStream(fileBytes);
        var item = storage.SaveFile("report.pdf", "application/pdf", file);

        Assert.Equal("file", item.Kind);
        Assert.Equal("report.pdf", item.OriginalFileName);
        Assert.Equal("application/pdf", item.MimeType);
        Assert.Equal(fileBytes.Length, item.Size);
        Assert.Equal(Path.Combine(storage.InboxPath, item.StoredFileName!), item.LocalPath);
        Assert.Equal(fileBytes, File.ReadAllBytes(item.LocalPath!));
        Assert.Empty(Directory.EnumerateFiles(storage.IncomingPath));
    }

    [Fact]
    public void SaveFile_WithEqualOriginalNames_StoresDistinctContent()
    {
        var storage = new EagleShareStorage(_temporaryRoot);
        using var firstFile = new MemoryStream([1]);
        using var secondFile = new MemoryStream([2]);

        var firstItem = storage.SaveFile("report.pdf", "application/pdf", firstFile);
        var secondItem = storage.SaveFile("report.pdf", "application/pdf", secondFile);

        Assert.NotEqual(firstItem.StoredFileName, secondItem.StoredFileName);
        Assert.Equal(new byte[] { 1 }, File.ReadAllBytes(firstItem.LocalPath!));
        Assert.Equal(new byte[] { 2 }, File.ReadAllBytes(secondItem.LocalPath!));
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryRoot))
        {
            Directory.Delete(_temporaryRoot, recursive: true);
        }
    }
}