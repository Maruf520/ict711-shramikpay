using ShramikPay.Domain;
using Xunit;

namespace ShramikPay.Tests;

public class PayrollTests
{
    private readonly TestData _t = new();
    private static readonly PayPeriod Sept = new(2026, 9, 26);

    [Fact]
    public void Full_attendance_without_overtime_pays_gross_plus_bonus()
    {
        var w = _t.AddWorker();
        _t.Shifts(w, "08:00", "17:00", TestData.September2026WorkingDays);
        var slip = _t.Payroll.Calculate(w.Id, Sept, "hr");
        Assert.Equal(12500m, slip.Gross);
        Assert.Equal(500m, slip.AttendanceBonus);
        Assert.Equal(0m, slip.Deductions);
        Assert.Equal(13000m, slip.NetPay);
    }

    [Fact]
    public void All_overtime_hours_are_paid_not_capped_at_two_SP102_regression()
    {
        var w = _t.AddWorker();
        _t.Shifts(w, "08:00", "20:00", 1); // 12 h minus 1 h break = 11 h worked, 3 h overtime
        var slip = _t.Payroll.Calculate(w.Id, Sept, "hr");
        Assert.Equal(3m, slip.OvertimeHours);
        Assert.Equal(193m, slip.OvertimePay); // 3 x 64.42 = 193.26 -> Tk 193
    }

    [Fact]
    public void Shift_over_ten_hours_raises_a_daily_limit_alert()
    {
        var w = _t.AddWorker();
        _t.Shifts(w, "08:00", "20:00", 1);
        var slip = _t.Payroll.Calculate(w.Id, Sept, "hr");
        Assert.Contains("10-hour daily limit", Assert.Single(slip.ComplianceAlerts));
    }

    [Fact]
    public void More_than_sixty_hours_in_a_week_raises_a_weekly_limit_alert()
    {
        var w = _t.AddWorker();
        _t.Shifts(w, "07:00", "18:00", 7, 8, 9, 10, 12, 13); // ISO week 37: 6 x 10 h = 60 h, within the limit
        _t.Attendance.Record(w.Id, "2026-09-11", "08:00", "12:00", _t.Supervisor); // +3 h -> 63 h
        var slip = _t.Payroll.Calculate(w.Id, Sept, "hr");
        Assert.Contains("60-hour weekly limit", Assert.Single(slip.ComplianceAlerts));
    }

    [Fact]
    public void Absent_days_are_deducted_and_bonus_is_withheld()
    {
        var w = _t.AddWorker();
        _t.Shifts(w, "08:00", "17:00", TestData.September2026WorkingDays.Skip(2).ToArray());
        var slip = _t.Payroll.Calculate(w.Id, Sept, "hr");
        Assert.Equal(447m, slip.Deductions);
        Assert.Equal(0m, slip.AttendanceBonus);
        Assert.Equal(12053m, slip.NetPay);
    }

    [Fact]
    public void Basic_below_legal_minimum_blocks_payroll()
    {
        var w = _t.AddWorker(6000m);
        Assert.Throws<InvalidOperationException>(() => _t.Payroll.Calculate(w.Id, Sept, "hr"));
    }

    [Fact]
    public void Unknown_worker_is_reported_as_not_found()
    {
        Assert.Throws<KeyNotFoundException>(() => _t.Payroll.Calculate(99, Sept, "hr"));
    }

    [Fact]
    public void Approval_locks_the_month_against_recalculation_and_attendance_edits()
    {
        var w = _t.AddWorker();
        _t.Payroll.Run(Sept, "hr");
        Assert.Equal(1, _t.Payroll.Approve(2026, 9, "accounts"));
        Assert.Equal(PayslipStatus.Approved, _t.Payslips.Get(w.Id, 2026, 9).Status);
        Assert.Throws<InvalidOperationException>(() => _t.Payroll.Calculate(w.Id, Sept, "hr"));
        Assert.Throws<InvalidOperationException>(() => _t.Attendance.Record(w.Id, "2026-09-02", "08:00", "17:00", _t.Hr, "late fix"));
    }

    [Fact]
    public void Approving_an_empty_month_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => _t.Payroll.Approve(2026, 9, "accounts"));
    }

    [Fact]
    public void Kiosk_preview_matches_the_payroll_calculation()
    {
        var w = _t.AddWorker();
        _t.Shifts(w, "08:00", "19:00", 1, 2, 3);
        Assert.Equal(_t.Payroll.Calculate(w.Id, Sept, "hr").NetPay, _t.Payslips.Preview(w, Sept));
    }
}
