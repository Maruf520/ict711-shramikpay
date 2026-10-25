using ShramikPay.Domain;
using ShramikPay.Infrastructure;
using ShramikPay.Services;

namespace ShramikPay.Endpoints;

public record PayrollRequest(int Year, int Month, int WorkingDays);

public static class PayrollEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/payroll/run", (PayrollRequest req, PayrollService payroll, HttpContext http) =>
            Results.Ok(payroll.Run(new PayPeriod(req.Year, req.Month, req.WorkingDays), http.CurrentUser().Username)))
            .RequireRoles("HR");

        app.MapPost("/api/payroll/{year}/{month}/approve", (int year, int month, PayrollService payroll, HttpContext http) =>
            Results.Ok(new { approved = payroll.Approve(year, month, http.CurrentUser().Username) }))
            .RequireRoles("Accounts");

        app.MapGet("/api/payslips/{workerId}/{year}/{month}", (int workerId, int year, int month, PayslipService payslips, HttpContext http) =>
        {
            var user = http.CurrentUser();
            if (user.Role == "Worker" && user.WorkerId != workerId)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }
            var slip = payslips.Get(workerId, year, month);
            return slip == null ? Results.NotFound() : Results.Ok(slip);
        }).RequireRoles("Worker", "HR", "Accounts", "Auditor");

        app.MapPost("/api/payments/{workerId}/{year}/{month}", async (int workerId, int year, int month, PayslipService payslips, WalletPaymentService wallet, HttpContext http) =>
        {
            var slip = payslips.Get(workerId, year, month);
            return slip == null ? Results.NotFound() : Results.Ok(await wallet.DisburseAsync(slip, http.CurrentUser().Username));
        }).RequireRoles("Accounts");

        app.MapGet("/api/exports/salary-sheet/{year}/{month}", (int year, int month, SalarySheetExporter exporter, HttpContext http) =>
            Results.Ok(new { file = Path.GetFileName(exporter.Export(year, month, http.CurrentUser().Username)) }))
            .RequireRoles("HR", "Accounts");

        app.MapGet("/api/audit/verify", (AuditService audit) => Results.Ok(new { intact = audit.VerifyChain() }))
            .RequireRoles("Auditor");
    }
}
