namespace NetworkProgramming.Views;

public record AdminUserView(string Id, string Username, string DisplayName, string Status,
    DateTime CreatedAt, DateTime? LastLoginAt, bool Online);
