using System.Security.Claims;
using MongoDB.Bson;

namespace NetworkProgramming.Services;

public static class ClaimsExtensions
{
    public static ObjectId UserId(this ClaimsPrincipal p) => ObjectId.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
