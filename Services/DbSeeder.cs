using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using NetworkProgramming.Data;
using NetworkProgramming.Models;

namespace NetworkProgramming.Services;

public static class DbSeeder
{
    /// <summary>Tạo index cần thiết và admin mặc định (admin / Admin@123) nếu collection account-admin còn trống.</summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        await EnsureIndexesAsync(scope.ServiceProvider.GetRequiredService<IConfiguration>());

        var db = scope.ServiceProvider.GetRequiredService<appDbContext>();
        var pw = scope.ServiceProvider.GetRequiredService<PasswordService>();
        if (await db.AdminAccounts.AnyAsync()) return;
        var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        db.AdminAccounts.Add(new AdminAccount
        {
            Username = cfg["Admin:Username"] ?? "admin",
            DisplayName = "Quản trị viên",
            PasswordHash = pw.Hash(cfg["Admin:Password"] ?? throw new InvalidOperationException("Thiếu Admin__Password trong .env (xem .env.example)."))
        });
        await db.SaveChangesAsync();
    }

    // Mỗi cặp user chỉ có một conversation direct. Partial index vì conversation group không có DirectKey.
    static Task EnsureIndexesAsync(IConfiguration cfg)
    {
        var client = new MongoClient(cfg.GetConnectionString("MongoDb"));
        var convs = client.GetDatabase(cfg["MongoDb:Database"] ?? "App-chat").GetCollection<BsonDocument>("conversations");
        var key = Builders<BsonDocument>.IndexKeys.Ascending(nameof(Conversation.DirectKey));
        var opts = new CreateIndexOptions<BsonDocument>
        {
            Name = "uniq_directkey",
            Unique = true,
            PartialFilterExpression = new BsonDocument(nameof(Conversation.DirectKey), new BsonDocument("$type", "string")),
        };
        return convs.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(key, opts));
    }
}
