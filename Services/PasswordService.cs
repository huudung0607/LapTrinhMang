using Microsoft.AspNetCore.Identity;

namespace NetworkProgramming.Services;

public class PasswordService
{
    static readonly object Owner = new();
    readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(Owner, password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(Owner, hash, password) != PasswordVerificationResult.Failed;
}
