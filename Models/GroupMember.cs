using MongoDB.Bson;

namespace NetworkProgramming.Models;

/// <summary>Collection "group-members" - thành viên của một cuộc trò chuyện (cả direct lẫn group).</summary>
public class GroupMember
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public ObjectId ConversationId { get; set; }
    public ObjectId UserId { get; set; }
    public string Role { get; set; } = MemberRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public static class MemberRole
{
    public const string Member = "member";
    public const string Owner = "owner";
}
