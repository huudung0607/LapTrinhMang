using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetworkProgramming.Data;
using NetworkProgramming.Models;
using NetworkProgramming.Services;
using NetworkProgramming.Views;

namespace NetworkProgramming.Controllers;

[ApiController, Authorize]
public class FilesController(appDbContext db) : ControllerBase
{
    [HttpPost("api/upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file.Length is 0 or > 20_000_000) return BadRequest("File tối đa 20MB.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var stored = Guid.NewGuid().ToString("N") + ext;
        await using (var fs = System.IO.File.Create(Path.Combine(UploadStore.Folder, stored))) await file.CopyToAsync(fs);

        var att = new Attachment
        {
            UploaderId = User.UserId(),
            Kind = UploadStore.ImageExt.Contains(ext) ? MessageKind.Image : MessageKind.File,
            StoredName = stored, Url = "/files/" + stored,
            FileName = Path.GetFileName(file.FileName), Size = file.Length,
        };
        db.Attachments.Add(att);
        await db.SaveChangesAsync();
        return Ok(new UploadView(att.Id.ToString(), att.Kind, att.Url, att.FileName));
    }

    // Uploads nằm ngoài wwwroot nên chỉ người đã đăng nhập mới đọc được; file không phải ảnh luôn tải về (tránh stored XSS).
    [HttpGet("files/{name}")]
    public IActionResult Download(string name, string? n)
    {
        var safe = Path.GetFileName(name);
        var path = UploadStore.PathOf(safe);
        if (!System.IO.File.Exists(path)) return NotFound();
        var ext = Path.GetExtension(safe).ToLowerInvariant();
        return UploadStore.ImageExt.Contains(ext)
            ? PhysicalFile(path, $"image/{(ext == ".jpg" ? "jpeg" : ext[1..])}")
            : PhysicalFile(path, "application/octet-stream", string.IsNullOrWhiteSpace(n) ? safe : Path.GetFileName(n));
    }
}
