using System.Globalization;
using ShramikPay.Domain;

namespace ShramikPay.Services;

public record AttendanceSummary(int DaysPresent, decimal OvertimeHours, IReadOnlyList<string> Alerts);

/// <summary>Pure wage arithmetic shared by payroll and the payslip preview (single source of truth).</summary>
public static class PayCalculator
{
    public static AttendanceSummary Summarise(IEnumerable<AttendanceRecord> records)
    {
        var list = records.ToList();
        var alerts = new List<string>();
        decimal overtime = 0;
        foreach (var rec in list)
        {
            var hours = AttendanceService.HoursWorked(rec);
            overtime += Math.Max(0, hours - WageRules.StandardDailyHours);
            if (hours > WageRules.MaxDailyHours)
            {
                alerts.Add($"{rec.Date:yyyy-MM-dd}: {hours:0.##} h exceeds the 10-hour daily limit (BLA s.100/s.108)");
            }
        }
        var weeks = list.GroupBy(r => ISOWeek.GetWeekOfYear(r.Date.ToDateTime(TimeOnly.MinValue)));
        foreach (var week in weeks)
        {
            var total = week.Sum(AttendanceService.HoursWorked);
            if (total > WageRules.MaxWeeklyHours)
            {
                alerts.Add($"ISO week {week.Key}: {total:0.##} h exceeds the 60-hour weekly limit (BLA s.102)");
            }
        }
        return new AttendanceSummary(list.Count, overtime, alerts);
    }

    public static Payslip Build(Worker worker, PayPeriod period, AttendanceSummary summary)
    {
        var basic = worker.BasicWage;
        var slip = new Payslip
        {
            WorkerId = worker.Id,
            Year = period.Year,
            Month = period.Month,
            Basic = basic,
            Gross = WageRules.Gross(basic),
            OvertimeHours = summary.OvertimeHours,
            OvertimePay = WageRules.Taka(WageRules.OvertimeRate(basic) * summary.OvertimeHours),
            AttendanceBonus = summary.DaysPresent >= period.WorkingDays ? WageRules.AttendanceBonus : 0m,
            Deductions = WageRules.AbsenceDeduction(basic, Math.Max(0, period.WorkingDays - summary.DaysPresent)),
            Status = PayslipStatus.Draft
        };
        slip.NetPay = slip.Gross + slip.OvertimePay + slip.AttendanceBonus - slip.Deductions;
        slip.ComplianceAlerts.AddRange(summary.Alerts);
        return slip;
    }
}
