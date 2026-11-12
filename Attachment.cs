using MongoDB.Bson;

namespace NetworkProgramming.Models;

/// <summary>Collection "attachments" - file/ảnh đã upload (metadata; nội dung nằm trong thư mục uploads).</summary>
public class Attachment
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public ObjectId UploaderId { get; set; }
    public ObjectId? MessageId { get; set; }          // null cho tới khi được gắn vào tin nhắn
    public string Kind { get; set; } = MessageKind.File; // image | file
    public string StoredName { get; set; } = "";
    public string Url { get; set; } = "";             // /files/{StoredName}
    public string FileName { get; set; } = "";
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
