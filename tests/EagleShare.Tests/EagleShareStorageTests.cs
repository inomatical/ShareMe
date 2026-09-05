using System.IO;
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

    public void Dispose()
    {
        if (Directory.Exists(_temporaryRoot))
        {
            Directory.Delete(_temporaryRoot, recursive: true);
        }
    }
}