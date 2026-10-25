using System.Text.RegularExpressions;
using ShramikPay.Data;
using ShramikPay.Domain;
using ShramikPay.Infrastructure;
using ShramikPay.Security;
using ShramikPay.Services;

namespace ShramikPay.Endpoints;

public record WorkerRequest(string Name, string Nid, int Grade, decimal BasicWage, string WalletNumber);
public record AttendanceRequest(int WorkerId, string Date, string ClockIn, string ClockOut, string CorrectionReason);

public static class WorkerEndpoints
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);
    private static readonly Regex NidPattern = new(@"^(\d{10}|\d{13}|\d{17})$", RegexOptions.None, RegexTimeout);
    private static readonly Regex WalletPattern = new(@"^01[3-9]\d{8}$", RegexOptions.None, RegexTimeout);

    public static void Map(WebApplication app)
    {
        app.MapPost("/api/workers", (WorkerRequest req, InMemoryStore store, CryptoService crypto, AuditService audit, HttpContext http) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name) || !NidPattern.IsMatch(req.Nid ?? "") || !WalletPattern.IsMatch(req.WalletNumber ?? ""))
            {
                return Results.BadRequest(new { error = "Name, a 10/13/17-digit NID and an 11-digit mobile wallet number are required" });
            }
            if (req.BasicWage < WageRules.MinimumBasic)
            {
                return Results.BadRequest(new { error = $"Basic wage must be at least Tk {WageRules.MinimumBasic}" });
            }
            var worker = new Worker
            {
                Id = store.Workers.Count + 1,
                Name = req.Name.Trim(),
                NidEncrypted = crypto.Encrypt(req.Nid),
                Grade = req.Grade,
                BasicWage = req.BasicWage,
                WalletEncrypted = crypto.Encrypt(req.WalletNumber),
                WalletMasked = CryptoService.Mask(req.WalletNumber)
            };
            store.Workers.Add(worker);
            audit.Append(http.CurrentUser().Username, "WorkerCreated", $"worker {worker.Id} wallet {worker.WalletMasked}");
            return Results.Ok(new { worker.Id });
        }).RequireRoles("HR");

        app.MapPost("/api/attendance", (AttendanceRequest req, AttendanceService attendance, HttpContext http) =>
        {
            var rec = attendance.Record(req.WorkerId, req.Date, req.ClockIn, req.ClockOut, http.CurrentUser(), req.CorrectionReason);
            return Results.Ok(new { rec.WorkerId, Hours = AttendanceService.HoursWorked(rec) });
        }).RequireRoles("Supervisor", "HR");
    }
}
