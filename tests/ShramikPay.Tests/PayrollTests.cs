using Xunit;

namespace ShramikPay.Tests;

public class PayrollTests
{
    [Fact]
    public void Gross_wage_for_minimum_grade_is_12500()
    {
        var (store, _, payroll) = TestData.Payroll();
        var w = TestData.AddWorker(store);
        var slip = payroll.Calculate(w.Id, 2026, 9, 26, false, false, "hr");
        Assert.Equal(12500, slip.Gross);
    }

    [Fact]
    public void Overtime_hours_are_counted()
    {
        var (store, attendance, payroll) = TestData.Payroll();
        var w = TestData.AddWorker(store);
        attendance.Record(w.Id, "2026-09-01", "08:00", "18:00");
        var slip = payroll.Calculate(w.Id, 2026, 9, 26, false, false, "hr");
        Assert.Equal(1, slip.OvertimeHours);
    }
}
