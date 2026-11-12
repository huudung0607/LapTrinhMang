using MongoDB.Bson;

namespace NetworkProgramming.Models;

/// <summary>Collection "users" - tài khoản người dùng chat.</summary>
public class User
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Status { get; set; } = UserStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
}

public static class UserStatus
{
    public const string Pending = "pending";
    public const string Active = "active";
    public const string Locked = "locked";
}
