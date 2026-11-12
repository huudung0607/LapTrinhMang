using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using NetworkProgramming.Data;
using NetworkProgramming.Models;
using NetworkProgramming.Services;
using NetworkProgramming.Views;

namespace NetworkProgramming.Controllers;

[ApiController, Authorize, Route("api/messages")]
public class MessagesController(appDbContext db, ChatService chat) : ControllerBase
{
    /// <summary>200 tin nhắn gần nhất của cuộc trò chuyện với người dùng <paramref name="otherId"/>.</summary>
    [HttpGet("{otherId}")]
    public async Task<IActionResult> History(string otherId)
    {
        if (!ObjectId.TryParse(otherId, out var other)) return BadRequest();
        var me = User.UserId();
        var conv = await chat.FindDirectAsync(me, other);
        if (conv is null) return Ok(Array.Empty<MessageView>());

        var msgs = (await db.Messages.Where(m => m.ConversationId == conv.Id)
            .OrderByDescending(m => m.SentAt).Take(200).ToListAsync()).AsEnumerable().Reverse().ToList();
        var msgIds = msgs.Select(m => m.Id).ToList();
        var attIds = msgs.Where(m => m.AttachmentId != null).Select(m => m.AttachmentId!.Value).ToList();
        var atts = (await db.Attachments.Where(a => attIds.Contains(a.Id)).ToListAsync()).ToDictionary(a => a.Id);
        var reactions = (await db.MessageReactions.Where(r => msgIds.Contains(r.MessageId)).ToListAsync()).ToLookup(r => r.MessageId);

        return Ok(msgs.Select(m => MessageView.From(
            m, m.AttachmentId is { } aid && atts.TryGetValue(aid, out var a) ? a : null,
            reactions[m.Id], me.ToString(), other.ToString())));
    }

    /// <summary>Thả / bỏ cảm xúc (toggle). Trả về danh sách reaction hiện tại của tin nhắn.</summary>
    [HttpPost("{messageId}/reactions")]
    public async Task<IActionResult> React(string messageId, ReactRequest r)
    {
        var emoji = r.Emoji?.Trim() ?? "";
        if (!ObjectId.TryParse(messageId, out var mid) || emoji.Length is 0 or > 16) return BadRequest();
        var me = User.UserId();
        var msg = await db.Messages.FirstOrDefaultAsync(m => m.Id == mid);
        if (msg is null) return NotFound();
        if (!await db.GroupMembers.AnyAsync(g => g.ConversationId == msg.ConversationId && g.UserId == me)) return Forbid();

        var existing = await db.MessageReactions.FirstOrDefaultAsync(x => x.MessageId == mid && x.UserId == me && x.Emoji == emoji);
        if (existing is null) db.MessageReactions.Add(new MessageReaction { MessageId = mid, UserId = me, Emoji = emoji });
        else db.MessageReactions.Remove(existing);
        await db.SaveChangesAsync();

        var all = await db.MessageReactions.Where(x => x.MessageId == mid).ToListAsync();
        return Ok(all.Select(x => new ReactionView(x.UserId.ToString(), x.Emoji)));
    }
}
