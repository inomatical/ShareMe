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
        ArgumentNullException.ThrowIfNull(content);

        lock (_inboxLock)
        {
            Initialize();
            var item = new InboxItem(Guid.NewGuid().ToString("N"), "text", content, DateTimeOffset.UtcNow);
            var textPath = Path.Combine(InboxPath, $"{item.Id}.txt");

            WriteDurableFile(textPath, Encoding.UTF8.GetBytes(content));

            var items = JsonSerializer.Deserialize<List<InboxItem>>(File.ReadAllText(InboxMetadataPath)) ?? [];
            items.Add(item);
            WriteDurableFile(InboxMetadataPath, JsonSerializer.SerializeToUtf8Bytes(items));

            return item;
        }
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