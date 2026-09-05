using System.IO;
using System.Text;
using System.Text.Json;

namespace EagleShare;

/// <summary>
/// Defines EagleShare's durable local storage locations and creates them when
/// the application starts.
/// </summary>
public sealed class EagleShareStorage
{
    private readonly object _inboxLock = new();

    public const string ApplicationDirectoryName = "EagleShare";
    public const string IncomingDirectoryName = "Incoming";
    public const string InboxDirectoryName = "Inbox";
    public const string InboxMetadataFileName = "inbox.json";

    public EagleShareStorage(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        RootPath = rootPath;
        IncomingPath = Path.Combine(RootPath, IncomingDirectoryName);
        InboxPath = Path.Combine(RootPath, InboxDirectoryName);
        InboxMetadataPath = Path.Combine(RootPath, InboxMetadataFileName);
    }

    public string RootPath { get; }

    public string IncomingPath { get; }

    public string InboxPath { get; }

    public string InboxMetadataPath { get; }

    public static EagleShareStorage CreateDefault()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return new EagleShareStorage(Path.Combine(documentsPath, ApplicationDirectoryName));
    }

    public void Initialize()
    {
        Directory.CreateDirectory(RootPath);
        Directory.CreateDirectory(IncomingPath);
        Directory.CreateDirectory(InboxPath);

        if (File.Exists(InboxMetadataPath))
        {
            return;
        }

        using var metadataFile = new FileStream(
            InboxMetadataPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 1024,
            FileOptions.WriteThrough);
        var emptyInbox = Encoding.UTF8.GetBytes("[]");
        metadataFile.Write(emptyInbox);
        metadataFile.Flush(flushToDisk: true);
    }

    public InboxItem SaveText(string content)
    {
        return SaveItem("text", content, ".txt");
    }

    public InboxItem SaveUrl(string content)
    {
        return SaveItem("url", content, ".url");
    }

    public InboxItem SaveImage(
        string originalFileName,
        string? mimeType,
        Stream content,
        Action<long>? progress = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentNullException.ThrowIfNull(content);

        var safeFileName = Path.GetFileName(originalFileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            throw new ArgumentException("A file name is required.", nameof(originalFileName));
        }

        lock (_inboxLock)
        {
            Initialize();
            var itemId = Guid.NewGuid().ToString("N");
            var extension = Path.GetExtension(safeFileName);
            var storedFileName = $"{itemId}{extension}";
            var incomingPath = Path.Combine(IncomingPath, $"{itemId}.upload");
            var finalPath = Path.Combine(InboxPath, storedFileName);

            try
            {
                using (var output = new FileStream(
                           incomingPath,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.None,
                           bufferSize: 81920,
                           FileOptions.WriteThrough))
                {
                    var buffer = new byte[81920];
                    long bytesWritten = 0;
                    int bytesRead;
                    while ((bytesRead = content.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        output.Write(buffer, 0, bytesRead);
                        bytesWritten += bytesRead;
                        progress?.Invoke(bytesWritten);
                    }

                    output.Flush(flushToDisk: true);
                }

                File.Move(incomingPath, finalPath);
                var item = new InboxItem(
                    itemId,
                    "image",
                    safeFileName,
                    DateTimeOffset.UtcNow,
                    safeFileName,
                    mimeType,
                    storedFileName);
                AppendToMetadata(item);
                return item;
            }
            finally
            {
                if (File.Exists(incomingPath))
                {
                    File.Delete(incomingPath);
                }
            }
        }
    }

    public void CopyImageTo(InboxItem item, string destinationPath, bool overwrite = false)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        if (item.Kind != "image" || string.IsNullOrWhiteSpace(item.StoredFileName))
        {
            throw new ArgumentException("Only persisted image inbox items can be copied.", nameof(item));
        }

        var storedFileName = Path.GetFileName(item.StoredFileName);
        if (!string.Equals(storedFileName, item.StoredFileName, StringComparison.Ordinal))
        {
            throw new ArgumentException("The image storage name is invalid.", nameof(item));
        }

        var sourcePath = Path.Combine(InboxPath, storedFileName);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The received image is no longer available.", sourcePath);
        }

        File.Copy(sourcePath, destinationPath, overwrite);
    }

    private InboxItem SaveItem(string kind, string content, string extension)
    {
        ArgumentNullException.ThrowIfNull(content);

        lock (_inboxLock)
        {
            Initialize();
            var item = new InboxItem(Guid.NewGuid().ToString("N"), kind, content, DateTimeOffset.UtcNow);
            var contentPath = Path.Combine(InboxPath, $"{item.Id}{extension}");

            WriteDurableFile(contentPath, Encoding.UTF8.GetBytes(content));

            AppendToMetadata(item);

            return item;
        }
    }

    private void AppendToMetadata(InboxItem item)
    {
        var items = JsonSerializer.Deserialize<List<InboxItem>>(File.ReadAllText(InboxMetadataPath)) ?? [];
        items.Add(item);
        WriteDurableFile(InboxMetadataPath, JsonSerializer.SerializeToUtf8Bytes(items));
    }

    private static void WriteDurableFile(string path, byte[] content)
    {
        using var file = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 1024,
            FileOptions.WriteThrough);
        file.Write(content);
        file.Flush(flushToDisk: true);
    }
}