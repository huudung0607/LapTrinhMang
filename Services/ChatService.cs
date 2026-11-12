using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using NetworkProgramming.Data;
using NetworkProgramming.Models;
using NetworkProgramming.Views;

namespace NetworkProgramming.Services;

/// <summary>Logic dùng chung cho conversation 1-1 và gửi tin nhắn.</summary>
public class ChatService(appDbContext db)
{
    public static string DirectKey(ObjectId a, ObjectId b) =>
        string.CompareOrdinal(a.ToString(), b.ToString()) <= 0 ? $"{a}_{b}" : $"{b}_{a}";

    public Task<Conversation?> FindDirectAsync(ObjectId a, ObjectId b)
    {
        var key = DirectKey(a, b);
        return db.Conversations.FirstOrDefaultAsync(c => c.DirectKey == key);
    }

    public async Task<Conversation> GetOrCreateDirectAsync(ObjectId a, ObjectId b)
    {
        var conv = await FindDirectAsync(a, b);
        if (conv is not null) return conv;
        conv = new Conversation { Type = ConversationType.Direct, DirectKey = DirectKey(a, b), CreatedBy = a };
        db.Conversations.Add(conv);
        var members = new[] { new GroupMember { ConversationId = conv.Id, UserId = a }, new GroupMember { ConversationId = conv.Id, UserId = b } };
        db.GroupMembers.AddRange(members);
        try { await SaveAsync(); return conv; }
        catch (Exception e) when (Any(e, IsDuplicateKey))
        {
            // Người kia vừa tạo cùng lúc (unique index DirectKey): bỏ bản của mình, dùng bản đã có.
            foreach (var x in new object[] { conv, members[0], members[1] }) db.Entry(x).State = EntityState.Detached;
            return await FindDirectAsync(a, b) ?? throw new InvalidOperationException("Không tạo được conversation.");
        }
    }

    // Hai thao tác ghi đồng thời trên cùng document làm Mongo báo write conflict (tạm thời): đợi một chút rồi thử lại.
    async Task SaveAsync()
    {
        for (var i = 1; ; i++)
        {
            try { await db.SaveChangesAsync(); return; }
            catch (Exception e) when (i < 5 && Any(e, IsTransient)) { await Task.Delay(40 * i); }
        }
    }

    static bool Any(Exception? e, Func<Exception, bool> test) { for (; e is not null; e = e.InnerException) if (test(e)) return true; return false; }

    static bool IsTransient(Exception e) => e is MongoCommandException c && (c.Code == 112 || c.HasErrorLabel("TransientTransactionError"));

    static bool IsDuplicateKey(Exception e) => e switch
    {
        MongoCommandException c => c.Code == 11000,
        MongoWriteException w => w.WriteError.Category == ServerErrorCategory.DuplicateKey,
        MongoBulkWriteException b => b.WriteErrors.Any(x => x.Category == ServerErrorCategory.DuplicateKey),
        _ => false,
    };

    /// <summary>Gửi tin nhắn 1-1. Trả về null nếu không hợp lệ.</summary>
    public async Task<MessageView?> SendDirectAsync(ObjectId from, ObjectId to, string kind, string? text, string? url, string? fileName)
    {
        if (from == to) return null;
        if (kind is not (MessageKind.Text or MessageKind.Image or MessageKind.File)) return null;
        if (kind == MessageKind.Text
                ? string.IsNullOrWhiteSpace(text) || text.Length > 4000
                : string.IsNullOrEmpty(url) || !url.StartsWith("/files/")) return null;

        var ids = new List<ObjectId> { from, to };
        if (await db.Users.CountAsync(u => ids.Contains(u.Id) && u.Status == UserStatus.Active) != 2) return null; // cả hai phải còn hoạt động

        Attachment? att = null;
        if (kind != MessageKind.Text)
        {
            att = await db.Attachments.FirstOrDefaultAsync(a => a.Url == url && a.UploaderId == from && a.MessageId == null);
            if (att is null) return null;
        }

        var conv = await GetOrCreateDirectAsync(from, to);
        var m = new Message { ConversationId = conv.Id, SenderId = from, Kind = kind, Text = kind == MessageKind.Text ? text : null, AttachmentId = att?.Id };
        db.Messages.Add(m);
        if (att is not null) att.MessageId = m.Id;
        conv.LastSenderId = from; conv.LastKind = kind; conv.LastText = m.Text;
        conv.LastFileName = att?.FileName; conv.LastMessageAt = m.SentAt;
        await SaveAsync();
        return MessageView.From(m, att, [], from.ToString(), to.ToString());
    }
}
