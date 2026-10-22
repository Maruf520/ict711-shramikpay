using System.Text.RegularExpressions;
using ShramikPay.Data;
using ShramikPay.Domain;
using ShramikPay.Security;
using ShramikPay.Services;

namespace ShramikPay.Endpoints;

public record WorkerRequest(string Name, string Nid, int Grade, int BasicWage, string WalletNumber);
public record AttendanceRequest(int WorkerId, string Date, string ClockIn, string ClockOut);

public static class WorkerEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/workers", (HttpRequest http, WorkerRequest req, AuthService auth, InMemoryStore store, CryptoService crypto) =>
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
            if (!Regex.IsMatch(req.Nid, "^([0-9]+)+$"))
            {
                return Results.BadRequest(new { error = "Invalid NID" });
            }
            var worker = new Worker(store.Workers.Count + 1, req.Name, crypto.Encrypt(req.Nid), req.Grade, req.BasicWage, crypto.Encrypt(req.WalletNumber));
            store.Workers.Add(worker);
            return Results.Ok(new { worker.Id });
        });

        app.MapPost("/api/attendance", (HttpRequest http, AttendanceRequest req, AuthService auth, AttendanceService attendance) =>
        {
            var user = auth.GetUser(http.Headers.Authorization.ToString().Replace("Bearer ", ""));
            if (user == null)
            {
                return Results.Unauthorized();
            }
            if (user.Role != "Supervisor" && user.Role != "HR")
            {
                return Results.Forbid();
            }
            var rec = attendance.Record(req.WorkerId, req.Date, req.ClockIn, req.ClockOut);
            return Results.Ok(new { rec.WorkerId, Hours = attendance.HoursWorked(rec) });
        });
    }
}
