using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Extensions;
using NetworkProgramming.Controllers;
using NetworkProgramming.Data;
using NetworkProgramming.Models;
using NetworkProgramming.Routes;
using NetworkProgramming.Services;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;

// Bí mật (connection string, mật khẩu admin) nằm trong file .env (không commit); mẫu: .env.example.
// shortcut: parser .env tối giản (KEY=VALUE, bỏ dòng # và trống), cần quote/expand thì đổi sang DotNetEnv.
if (File.Exists(".env"))
    foreach (var line in File.ReadAllLines(".env"))
    {
        var l = line.Trim();
        var i = l.IndexOf('=');
        if (l.Length == 0 || l[0] == '#' || i < 1) continue;
        var k = l[..i].Trim();
        if (Environment.GetEnvironmentVariable(k) is null) Environment.SetEnvironmentVariable(k, l[(i + 1)..].Trim());
    }

var builder = WebApplication.CreateBuilder(args);

var mongoCs = builder.Configuration.GetConnectionString("MongoDb")
    ?? throw new InvalidOperationException("Thiếu ConnectionStrings:MongoDb trong .env (xem .env.example).");
var mongoDb = builder.Configuration["MongoDb:Database"] ?? "App-chat";

builder.Services.AddDbContext<appDbContext>(o => o.UseMongoDB(mongoCs, mongoDb));
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.Name = "chat_auth";
        o.ExpireTimeSpan = TimeSpan.FromDays(7);
        o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return Task.CompletedTask; };
        o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = 403; return Task.CompletedTask; };
        // Tài khoản bị khóa/xóa sẽ mất quyền truy cập ở request kế tiếp.
        o.Events.OnValidatePrincipal = async c =>
        {
            var db = c.HttpContext.RequestServices.GetRequiredService<appDbContext>();
            var principal = c.Principal!;
            var id = ObjectId.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ok = principal.IsInRole(AuthController.AdminRole)
                ? await db.AdminAccounts.AnyAsync(a => a.Id == id)
                : await db.Users.AnyAsync(u => u.Id == id && u.Status == UserStatus.Active);
            if (!ok)
            {
                c.RejectPrincipal();
                await c.HttpContext.SignOutAsync();
            }
        };
    });

var app = builder.Build();
Directory.CreateDirectory(UploadStore.Folder);
await DbSeeder.SeedAsync(app.Services);

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapAppRoutes();
app.Run();
