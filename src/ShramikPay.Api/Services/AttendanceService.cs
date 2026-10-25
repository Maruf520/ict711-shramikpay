using System.Globalization;
using ShramikPay.Data;
using ShramikPay.Domain;

namespace ShramikPay.Services;

public class AttendanceService
{
    private readonly InMemoryStore _store;
    private readonly AuditService _audit;

    public AttendanceService(InMemoryStore store, AuditService audit)
    {
        _store = store;
        _audit = audit;
    }

    public AttendanceRecord Record(int workerId, string date, string clockIn, string clockOut, User by, string correctionReason = null)
    {
        if (!_store.Workers.Exists(w => w.Id == workerId))
        {
            throw new KeyNotFoundException("Worker not found");
        }
        var day = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (_store.LockedPeriods.Contains((day.Year, day.Month)))
        {
            throw new InvalidOperationException("Payroll for this month is approved; attendance is locked");
        }
        var rec = new AttendanceRecord
        {
            WorkerId = workerId,
            Date = day,
            ClockIn = day.ToDateTime(TimeOnly.ParseExact(clockIn, "HH:mm", CultureInfo.InvariantCulture)),
            ClockOut = day.ToDateTime(TimeOnly.ParseExact(clockOut, "HH:mm", CultureInfo.InvariantCulture)),
            RecordedBy = by.Username
        };
        if (rec.ClockOut <= rec.ClockIn)
        {
            throw new ArgumentException("Clock-out must be after clock-in");
        }
        var existing = _store.Attendance.Find(a => a.WorkerId == workerId && a.Date == day);
        if (existing != null)
        {
            if (by.Role != "HR" || string.IsNullOrWhiteSpace(correctionReason))
            {
                throw new UnauthorizedAccessException("Only HR can correct attendance, and a reason is required");
            }
            _store.Attendance.Remove(existing);
            _audit.Append(by.Username, "AttendanceCorrected", $"worker {workerId} {day:yyyy-MM-dd} {existing.ClockIn:HH:mm}-{existing.ClockOut:HH:mm} -> {clockIn}-{clockOut}; reason: {correctionReason}");
        }
        else
        {
            _audit.Append(by.Username, "AttendanceRecorded", $"worker {workerId} {day:yyyy-MM-dd} {clockIn}-{clockOut}");
        }
        _store.Attendance.Add(rec);
        return rec;
    }

    public static decimal HoursWorked(AttendanceRecord rec)
    {
        var hours = (decimal)(rec.ClockOut - rec.ClockIn).TotalHours - WageRules.BreakHours;
        return Math.Max(0, hours);
    }

    public List<AttendanceRecord> ForMonth(int workerId, int year, int month) =>
        _store.Attendance.Where(a => a.WorkerId == workerId && a.Date.Year == year && a.Date.Month == month)
            .OrderBy(a => a.Date).ToList();
}
