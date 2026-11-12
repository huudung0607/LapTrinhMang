namespace NetworkProgramming.Views;

public record RegisterRequest(string? Username, string? DisplayName, string? Password);
public record LoginRequest(string? Username, string? Password);
public record SessionView(string Id, string DisplayName, string Role);
