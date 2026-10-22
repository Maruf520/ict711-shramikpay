using ShramikPay.Services;

namespace ShramikPay.Endpoints;

public record LoginRequest(string Username, string Password);
public record RegisterRequest(string Username, string Password, string Role, int? WorkerId);

public static class AuthEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/auth/login", (LoginRequest req, AuthService auth) =>
        {
            var token = auth.Login(req.Username, req.Password);
            return token == null ? Results.Unauthorized() : Results.Ok(new { token });
        });
        app.MapPost("/api/auth/register", (RegisterRequest req, AuthService auth) =>
        {
            var user = auth.Register(req.Username, req.Password, req.Role, req.WorkerId);
            return Results.Ok(new { user.Username, user.Role });
        });
    }
}
