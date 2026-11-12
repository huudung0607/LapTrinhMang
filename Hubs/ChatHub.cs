using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using NetworkProgramming.Data;
using NetworkProgramming.Services;

namespace NetworkProgramming.Hubs;

public class ChatHub(appDbContext db, ChatService chat) : Hub
{
    static readonly ConcurrentDictionary<string, int> Conn = new(); // userId -> số tab đang mở
    public static bool IsOnline(string userId) => Conn.ContainsKey(userId);

    string Me => Context.UserIdentifier!;

    public override async Task OnConnectedAsync()
    {
        if (Conn.AddOrUpdate(Me, 1, (_, n) => n + 1) == 1) await Clients.Others.SendAsync("presence", Me, true);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? e)
    {
        var left = Conn.AddOrUpdate(Me, 0, (_, n) => n - 1);
        if (left <= 0)
        {
            Conn.TryRemove(Me, out _);
            var id = ObjectId.Parse(Me);
            var u = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (u is not null) { u.LastSeenAt = DateTime.UtcNow; await db.SaveChangesAsync(); }
            await Clients.Others.SendAsync("presence", Me, false);
        }
        await base.OnDisconnectedAsync(e);
    }

    public async Task Send(string toId, string kind, string? text, string? url, string? fileName)
    {
        if (!ObjectId.TryParse(toId, out var to)) return;
        var m = await chat.SendDirectAsync(ObjectId.Parse(Me), to, kind, text, url, fileName);
        if (m is not null) await Clients.Users(Me, toId).SendAsync("message", m);
    }
}
