using MongoDB.Bson;

namespace NetworkProgramming.Models;

/// <summary>Collection "messages".</summary>
public class Message
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public ObjectId ConversationId { get; set; }
    public ObjectId SenderId { get; set; }
    public string Kind { get; set; } = MessageKind.Text;
    public string? Text { get; set; }
    public ObjectId? AttachmentId { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}

public static class MessageKind
{
    public const string Text = "text";
    public const string Image = "image";
    public const string File = "file";
}
