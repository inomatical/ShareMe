namespace EagleShare;

public sealed record InboxItem(
	string Id,
	string Kind,
	string Content,
	DateTimeOffset ReceivedAt,
	string? OriginalFileName = null,
	string? MimeType = null,
	string? StoredFileName = null,
	long? Size = null,
	string? LocalPath = null);