namespace EagleShare;

public sealed record InboxItem(string Id, string Kind, string Content, DateTimeOffset ReceivedAt);