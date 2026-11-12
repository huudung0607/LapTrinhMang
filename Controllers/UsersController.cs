using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkProgramming.Data;
using NetworkProgramming.Hubs;
using NetworkProgramming.Models;
using NetworkProgramming.Services;
using NetworkProgramming.Views;

namespace NetworkProgramming.Controllers;

[ApiController, Authorize, Route("api/users")]
public class UsersController(appDbContext db) : ControllerBase
{
    /// <summary>Danh sách liên hệ: mọi user đang hoạt động + tin nhắn cuối.</summary>
    [HttpGet]
    public async Task<IEnumerable<ContactView>> Contacts()
    {
        var me = User.UserId();
        var users = await db.Users.Where(u => u.Status == UserStatus.Active && u.Id != me).ToListAsync();

        var myConvIds = (await db.GroupMembers.Where(m => m.UserId == me).ToListAsync()).Select(m => m.ConversationId).ToList();
        var convs = await db.Conversations.Where(c => myConvIds.Contains(c.Id) && c.Type == ConversationType.Direct).ToListAsync();
        var byKey = convs.Where(c => c.DirectKey != null).ToDictionary(c => c.DirectKey!);

        return users.Select(u =>
        {
            byKey.TryGetValue(ChatService.DirectKey(me, u.Id), out var c);
            var last = c?.LastMessageAt is null ? null
                : new LastMessageView(c.LastKind, c.LastText, c.LastFileName, c.LastMessageAt.Value, c.LastSenderId == me);
            return new ContactView(u.Id.ToString(), u.DisplayName, ChatHub.IsOnline(u.Id.ToString()), u.LastSeenAt, last);
        })
        .OrderByDescending(x => x.Last?.SentAt ?? DateTime.MinValue).ThenBy(x => x.DisplayName)
        .ToList();
    }
}
