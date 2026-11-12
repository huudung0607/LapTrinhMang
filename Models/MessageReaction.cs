using MongoDB.Bson;

namespace NetworkProgramming.Models;

/// <summary>Collection "message-reactions".</summary>
public class MessageReaction
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public ObjectId MessageId { get; set; }
    public ObjectId UserId { get; set; }
    public string Emoji { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
