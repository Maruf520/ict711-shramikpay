using ShramikPay.Data;
using ShramikPay.Services;

namespace ShramikPay.Endpoints;

public record PayrollRequest(int Year, int Month, int WorkingDays);

public static class PayrollEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/payroll/run", (HttpRequest http, PayrollRequest req, AuthService auth, InMemoryStore store, PayrollService payroll) =>
        {
            var user = auth.GetUser(http.Headers.Authorization.ToString().Replace("Bearer ", ""));
            if (user == null)
            {
                return Results.Unauthorized();
            }
            if (user.Role != "HR")
            {
                return Results.Forbid();
            }
            var slips = store.Workers.Select(w => payroll.Calculate(w.Id, req.Year, req.Month, req.WorkingDays, true, true, user.Username)).ToList();
            return Results.Ok(slips);
        });

        app.MapGet("/api/payslips/{workerId}/{year}/{month}", (HttpRequest http, int workerId, int year, int month, AuthService auth, PayslipService payslips) =>
        {
            var user = auth.GetUser(http.Headers.Authorization.ToString().Replace("Bearer ", ""));
            if (user == null)
            {
                return Results.Unauthorized();
            }
            var slip = payslips.Get(workerId, year, month);
            return slip == null ? Results.NotFound() : Results.Ok(slip);
        });

        app.MapPost("/api/payments/{workerId}/{year}/{month}", (HttpRequest http, int workerId, int year, int month, AuthService auth, PayslipService payslips, WalletPaymentService wallet) =>
        {
            var user = auth.GetUser(http.Headers.Authorization.ToString().Replace("Bearer ", ""));
            if (user == null)
            {
                return Results.Unauthorized();
            }
            if (user.Role != "Accounts")
            {
                return Results.Forbid();
            }
            var slip = payslips.Get(workerId, year, month);
            if (slip == null)
            {
                return Results.NotFound();
            }
            return Results.Ok(wallet.Disburse(slip));
        });
    }
}
