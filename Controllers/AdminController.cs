using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using NetworkProgramming.Data;
using NetworkProgramming.Hubs;
using NetworkProgramming.Models;
using NetworkProgramming.Services;
using NetworkProgramming.Views;

namespace NetworkProgramming.Controllers;

[ApiController, Authorize(Roles = AuthController.AdminRole), Route("api/admin")]
public class AdminController(appDbContext db) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IEnumerable<AdminUserView>> Users() =>
        (await db.Users.OrderBy(u => u.CreatedAt).ToListAsync())
        .OrderBy(u => u.Status == UserStatus.Pending ? 0 : 1)
        .Select(u => new AdminUserView(u.Id.ToString(), u.Username, u.DisplayName, u.Status, u.CreatedAt, u.LastLoginAt, ChatHub.IsOnline(u.Id.ToString())));

    // op: approve | unlock | lock
    [HttpPost("users/{id}/{op}")]
    public async Task<IActionResult> SetStatus(string id, string op)
    {
        var status = op switch { "approve" or "unlock" => UserStatus.Active, "lock" => UserStatus.Locked, _ => null };
        if (status is null || !ObjectId.TryParse(id, out var uid)) return BadRequest();
        var u = await db.Users.FirstOrDefaultAsync(x => x.Id == uid);
        if (u is null) return NotFound();
        u.Status = status; await db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>Xóa tài khoản cùng các cuộc trò chuyện 1-1, tin nhắn, file đính kèm và reaction liên quan.</summary>
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        if (!ObjectId.TryParse(id, out var uid)) return BadRequest();
        var u = await db.Users.FirstOrDefaultAsync(x => x.Id == uid);
        if (u is null) return NotFound();

        var memberships = await db.GroupMembers.Where(g => g.UserId == uid).ToListAsync();
        var convIds = memberships.Select(g => g.ConversationId).ToList();
        var directs = await db.Conversations.Where(c => convIds.Contains(c.Id) && c.Type == ConversationType.Direct).ToListAsync();
        var directIds = directs.Select(c => c.Id).ToList();

        // Tin nhắn trong các cuộc 1-1 của user + tin nhắn user đã gửi ở nơi khác.
        var msgs = await db.Messages.Where(m => directIds.Contains(m.ConversationId) || m.SenderId == uid).ToListAsync();
        var msgIds = msgs.Select(m => m.Id).ToList();
        var atts = await db.Attachments.Where(a => a.UploaderId == uid || (a.MessageId != null && msgIds.Contains(a.MessageId.Value))).ToListAsync();
        var reactions = await db.MessageReactions.Where(r => r.UserId == uid || msgIds.Contains(r.MessageId)).ToListAsync();
        var directMembers = await db.GroupMembers.Where(g => directIds.Contains(g.ConversationId)).ToListAsync();

        foreach (var a in atts) { try { System.IO.File.Delete(UploadStore.PathOf(a.StoredName)); } catch { /* bỏ qua file đã mất */ } }
        db.MessageReactions.RemoveRange(reactions);
        db.Attachments.RemoveRange(atts);
        db.Messages.RemoveRange(msgs);
        db.GroupMembers.RemoveRange(directMembers);
        db.GroupMembers.RemoveRange(memberships.Where(g => !directIds.Contains(g.ConversationId)));
        db.Conversations.RemoveRange(directs);
        db.Users.Remove(u);
        await db.SaveChangesAsync();
        return Ok();
    }
}
