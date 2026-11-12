using NetworkProgramming.Hubs;

namespace NetworkProgramming.Routes;

/// <summary>
/// Nơi tập trung các route của ứng dụng:
///   /api/register, /api/login, /api/logout, /api/me       -> AuthController
///   /api/users                                            -> UsersController
///   /api/messages/{otherId}, /api/messages/{id}/reactions -> MessagesController
///   /api/upload, /files/{name}                            -> FilesController
///   /api/admin/...                                        -> AdminController
///   /chat (SignalR)                                       -> ChatHub
/// </summary>
public static class AppRoutes
{
    public const string ChatHubPath = "/chat";

    public static WebApplication MapAppRoutes(this WebApplication app)
    {
        app.MapControllers();
        app.MapHub<ChatHub>(ChatHubPath).RequireAuthorization();
        return app;
    }
}
