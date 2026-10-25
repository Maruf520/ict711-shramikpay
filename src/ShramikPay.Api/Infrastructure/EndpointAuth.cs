using ShramikPay.Domain;
using ShramikPay.Services;

namespace ShramikPay.Infrastructure;

/// <summary>Single authentication/authorisation guard for every protected endpoint.</summary>
public static class EndpointAuth
{
    private const string BearerPrefix = "Bearer ";
    private const string UserKey = "ShramikPay.User";

    public static RouteHandlerBuilder RequireRoles(this RouteHandlerBuilder builder, params string[] roles) =>
        builder.AddEndpointFilter(async (ctx, next) =>
        {
            var http = ctx.HttpContext;
            var header = http.Request.Headers.Authorization.ToString();
            var token = header.StartsWith(BearerPrefix, StringComparison.Ordinal) ? header[BearerPrefix.Length..] : null;
            var user = http.RequestServices.GetRequiredService<AuthService>().GetUser(token);
            if (user == null)
            {
                return Results.Unauthorized();
            }
            if (roles.Length > 0 && !roles.Contains(user.Role))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }
            http.Items[UserKey] = user;
            return await next(ctx);
        });

    public static User CurrentUser(this HttpContext http) => (User)http.Items[UserKey];
}
