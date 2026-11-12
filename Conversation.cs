using MongoDB.Bson;

namespace NetworkProgramming.Models;

/// <summary>Collection "conversations" - cuộc trò chuyện 1-1 (direct) hoặc nhóm (group).</summary>
public class Conversation
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string Type { get; set; } = ConversationType.Direct;
    public string? Name { get; set; }                 // chỉ dùng cho group
    public string? DirectKey { get; set; }            // "idNhỏ_idLớn" - chỉ dùng cho direct, để tìm nhanh
    public ObjectId CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Tin nhắn cuối (denormalize để danh sách liên hệ không phải join).
    public ObjectId? LastSenderId { get; set; }
    public string? LastKind { get; set; }
    public string? LastText { get; set; }
    public string? LastFileName { get; set; }
    public DateTime? LastMessageAt { get; set; }
}

public static class ConversationType
{
    public const string Direct = "direct";
    public const string Group = "group";
}
