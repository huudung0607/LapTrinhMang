using NetworkProgramming.Models;

namespace NetworkProgramming.Views;

public record LastMessageView(string? Kind, string? Text, string? FileName, DateTime SentAt, bool Mine);

public record ContactView(string Id, string DisplayName, bool Online, DateTime? LastSeenAt, LastMessageView? Last);

public record ReactionView(string UserId, string Emoji);

public record MessageView(
    string Id, string ConversationId, string FromId, string ToId,
    string Kind, string? Text, string? Url, string? FileName, DateTime SentAt,
    List<ReactionView> Reactions)
{
    /// <param name="me">người xem</param><param name="other">người còn lại trong cuộc trò chuyện 1-1</param>
    public static MessageView From(Message m, Attachment? att, IEnumerable<MessageReaction> reactions, string me, string other)
    {
        var from = m.SenderId.ToString();
        return new(m.Id.ToString(), m.ConversationId.ToString(), from, from == me ? other : me,
            m.Kind, m.Text, att?.Url, att?.FileName, m.SentAt,
            reactions.Select(r => new ReactionView(r.UserId.ToString(), r.Emoji)).ToList());
    }
}

public record ReactRequest(string? Emoji);

public record UploadView(string Id, string Kind, string Url, string FileName);
