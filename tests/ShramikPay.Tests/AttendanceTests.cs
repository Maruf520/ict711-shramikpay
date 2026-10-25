using ShramikPay.Services;
using Xunit;

namespace ShramikPay.Tests;

public class AttendanceTests
{
    private readonly TestData _t = new();

    [Fact]
    public void Records_hours_worked_net_of_the_meal_break()
    {
        var w = _t.AddWorker();
        var rec = _t.Attendance.Record(w.Id, "2026-09-01", "08:00", "17:30", _t.Supervisor);
        Assert.Equal(8.5m, AttendanceService.HoursWorked(rec));
        Assert.Equal("sup.karim", rec.RecordedBy);
    }

    [Fact]
    public void Clock_out_before_clock_in_is_rejected_SP102_regression()
    {
        var w = _t.AddWorker();
        Assert.Throws<ArgumentException>(() => _t.Attendance.Record(w.Id, "2026-09-01", "17:00", "08:00", _t.Supervisor));
    }

    [Fact]
    public void Unknown_worker_does_not_crash_with_null_reference_SP102_regression()
    {
        Assert.Throws<KeyNotFoundException>(() => _t.Attendance.Record(42, "2026-09-01", "08:00", "17:00", _t.Supervisor));
    }

    [Fact]
    public void Ambiguous_local_date_formats_are_rejected()
    {
        var w = _t.AddWorker();
        Assert.Throws<FormatException>(() => _t.Attendance.Record(w.Id, "02/09/2026", "08:00", "17:00", _t.Supervisor));
    }

    [Fact]
    public void Supervisor_cannot_overwrite_an_existing_record()
    {
        var w = _t.AddWorker();
        _t.Attendance.Record(w.Id, "2026-09-01", "08:00", "17:00", _t.Supervisor);
        Assert.Throws<UnauthorizedAccessException>(() => _t.Attendance.Record(w.Id, "2026-09-01", "08:00", "13:00", _t.Supervisor));
    }

    [Fact]
    public void Hr_correction_requires_a_reason_and_is_audited()
    {
        var w = _t.AddWorker();
        _t.Attendance.Record(w.Id, "2026-09-01", "08:00", "17:00", _t.Supervisor);
        Assert.Throws<UnauthorizedAccessException>(() => _t.Attendance.Record(w.Id, "2026-09-01", "08:00", "19:00", _t.Hr));
        _t.Attendance.Record(w.Id, "2026-09-01", "08:00", "19:00", _t.Hr, "Overtime approved by line chief");
        var entry = _t.Store.Audit[^1];
        Assert.Equal("AttendanceCorrected", entry.Action);
        Assert.Contains("08:00-17:00 -> 08:00-19:00", entry.Details);
        Assert.Single(_t.Attendance.ForMonth(w.Id, 2026, 9));
    }
}
