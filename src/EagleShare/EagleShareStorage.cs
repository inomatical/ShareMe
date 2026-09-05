using System.IO;
using System.Text;

namespace EagleShare;

/// <summary>
/// Defines EagleShare's durable local storage locations and creates them when
/// the application starts.
/// </summary>
public sealed class EagleShareStorage
{
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
}