using MongoDB.Bson;

namespace NetworkProgramming.Models;

/// <summary>Collection "account-admin" - tài khoản quản trị.</summary>
public class AdminAccount
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
