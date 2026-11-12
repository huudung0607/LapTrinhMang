using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkProgramming.Data;
using NetworkProgramming.Models;
using NetworkProgramming.Services;
using NetworkProgramming.Views;

namespace NetworkProgramming.Controllers;

[ApiController, Route("api")]
public class AuthController(appDbContext db, PasswordService passwords) : ControllerBase
{
    public const string AdminRole = "admin", UserRole = "user";
    const int MaxPasswordLength = 128; // chặn băm mật khẩu khổng lồ làm tốn CPU

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest r)
    {
        var name = r.Username?.Trim().ToLowerInvariant() ?? "";
        var display = r.DisplayName?.Trim() ?? "";
        if (name.Length < 3 || name.Length > 30 || !name.All(char.IsAsciiLetterOrDigit) || (r.Password?.Length ?? 0) is < 6 or > MaxPasswordLength || display.Length == 0)
            return BadRequest("Tên đăng nhập 3-30 ký tự chữ/số, mật khẩu 6-128 ký tự, cần có tên hiển thị.");
        if (await db.Users.AnyAsync(u => u.Username == name) || await db.AdminAccounts.AnyAsync(a => a.Username == name))
            return Conflict("Tên đăng nhập đã tồn tại.");

        db.Users.Add(new User { Username = name, DisplayName = display[..Math.Min(40, display.Length)], PasswordHash = passwords.Hash(r.Password!) });
        await db.SaveChangesAsync();
        return Ok("Đăng ký thành công. Vui lòng chờ quản trị viên duyệt tài khoản.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest r)
    {
        var name = (r.Username ?? "").Trim().ToLowerInvariant();
        var pass = r.Password ?? "";
        const string bad = "Sai tên đăng nhập hoặc mật khẩu.";
        if (pass.Length > MaxPasswordLength) return BadRequest(bad);

        var admin = await db.AdminAccounts.FirstOrDefaultAsync(a => a.Username == name);
        if (admin is not null)
        {
            if (!passwords.Verify(admin.PasswordHash, pass)) return BadRequest(bad);
            admin.LastLoginAt = DateTime.UtcNow; await db.SaveChangesAsync();
            await SignIn(admin.Id.ToString(), admin.DisplayName, AdminRole);
            return Ok(new SessionView(admin.Id.ToString(), admin.DisplayName, AdminRole));
        }

        var u = await db.Users.FirstOrDefaultAsync(x => x.Username == name);
        if (u is null || !passwords.Verify(u.PasswordHash, pass)) return BadRequest(bad);
        if (u.Status == UserStatus.Pending) return BadRequest("Tài khoản đang chờ quản trị viên duyệt.");
        if (u.Status == UserStatus.Locked) return BadRequest("Tài khoản đã bị khóa.");
        u.LastLoginAt = DateTime.UtcNow; await db.SaveChangesAsync();
        await SignIn(u.Id.ToString(), u.DisplayName, UserRole);
        return Ok(new SessionView(u.Id.ToString(), u.DisplayName, UserRole));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(); return Ok(); }

    [Authorize, HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var id = User.UserId();
        if (User.IsInRole(AdminRole))
        {
            var a = await db.AdminAccounts.FirstAsync(x => x.Id == id);
            return Ok(new SessionView(a.Id.ToString(), a.DisplayName, AdminRole));
        }
        var u = await db.Users.FirstAsync(x => x.Id == id);
        return Ok(new SessionView(u.Id.ToString(), u.DisplayName, UserRole));
    }

    Task SignIn(string id, string displayName, string role)
    {
        var identity = new ClaimsIdentity(
            [new(ClaimTypes.NameIdentifier, id), new(ClaimTypes.Name, displayName), new(ClaimTypes.Role, role)],
            CookieAuthenticationDefaults.AuthenticationScheme);
        return HttpContext.SignInAsync(new ClaimsPrincipal(identity));
    }
}
