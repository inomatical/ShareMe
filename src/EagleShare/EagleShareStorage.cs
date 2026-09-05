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

    public IReadOnlyList<InboxItem> LoadInbox()
    {
        lock (_inboxLock)
        {
            Initialize();
            var items = JsonSerializer.Deserialize<List<InboxItem>>(File.ReadAllText(InboxMetadataPath)) ?? [];
            var restoredItems = new List<InboxItem>();

            foreach (var item in items)
            {
                if (!TryGetCompletedContentPath(item, out var contentPath))
                {
                    continue;
                }

                var restoredItem = item.Kind switch
                {
                    "text" or "url" => item with
                    {
                        Content = File.ReadAllText(contentPath),
                        LocalPath = contentPath,
                        Size = new FileInfo(contentPath).Length,
                    },
                    "image" or "file" => item with
                    {
                        LocalPath = contentPath,
                        Size = new FileInfo(contentPath).Length,
                    },
                    _ => item,
                };
                restoredItems.Add(restoredItem);
            }

            return restoredItems;
        }
    }

    public bool DeleteTextOrUrl(InboxItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Kind is not ("text" or "url"))
        {
            throw new ArgumentException("Only text and URL inbox items can be deleted.", nameof(item));
        }

        lock (_inboxLock)
        {
            Initialize();
            var items = JsonSerializer.Deserialize<List<InboxItem>>(File.ReadAllText(InboxMetadataPath)) ?? [];
            var removedCount = items.RemoveAll(storedItem => storedItem.Id == item.Id && storedItem.Kind == item.Kind);
            if (removedCount != 1)
            {
                return false;
            }

            if (!TryGetCompletedContentPath(item, out var contentPath))
            {
                throw new InvalidOperationException("The inbox item's stored record is invalid.");
            }

            File.Delete(contentPath);
            WriteDurableFile(InboxMetadataPath, JsonSerializer.SerializeToUtf8Bytes(items));
            return true;
        }
    }

    public InboxItem SaveImage(
        string originalFileName,
        string? mimeType,
        Stream content,
        Action<long>? progress = null)
    {
        return SaveBinaryItem("image", originalFileName, mimeType, content, progress);
    }

    public InboxItem SaveFile(
        string originalFileName,
        string? mimeType,
        Stream content,
        Action<long>? progress = null)
    {
        return SaveBinaryItem("file", originalFileName, mimeType, content, progress);
    }

    private InboxItem SaveBinaryItem(
        string kind,
        string originalFileName,
        string? mimeType,
        Stream content,
        Action<long>? progress)
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
            long bytesWritten = 0;

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
                    kind,
                    safeFileName,
                    DateTimeOffset.UtcNow,
                    safeFileName,
                    mimeType,
                    storedFileName,
                    bytesWritten,
                    finalPath);
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
        CopyBinaryItemTo(item, "image", destinationPath, overwrite);
    }

    public void CopyFileTo(InboxItem item, string destinationPath, bool overwrite = false)
    {
        CopyBinaryItemTo(item, "file", destinationPath, overwrite);
    }

    private void CopyBinaryItemTo(InboxItem item, string expectedKind, string destinationPath, bool overwrite)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        if (item.Kind != expectedKind || string.IsNullOrWhiteSpace(item.StoredFileName))
        {
            throw new ArgumentException($"Only persisted {expectedKind} inbox items can be copied.", nameof(item));
        }

        var storedFileName = Path.GetFileName(item.StoredFileName);
        if (!string.Equals(storedFileName, item.StoredFileName, StringComparison.Ordinal))
        {
            throw new ArgumentException("The image storage name is invalid.", nameof(item));
        }

        var sourcePath = Path.Combine(InboxPath, storedFileName);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"The received {expectedKind} is no longer available.", sourcePath);
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

    private bool TryGetCompletedContentPath(InboxItem item, out string contentPath)
    {
        var fileName = item.Kind switch
        {
            "text" => $"{item.Id}.txt",
            "url" => $"{item.Id}.url",
            "image" or "file" => item.StoredFileName,
            _ => null,
        };

        if (string.IsNullOrWhiteSpace(fileName) || !string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal))
        {
            contentPath = string.Empty;
            return false;
        }

        contentPath = Path.Combine(InboxPath, fileName);
        return File.Exists(contentPath);
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