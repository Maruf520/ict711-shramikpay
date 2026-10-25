namespace ShramikPay.Services;

/// <summary>
/// Wage rules: Bangladesh Labour Act 2006 (s.100, s.102, s.108) and the 2023 RMG minimum wage
/// (lowest grade: gross Tk 12,500, basic Tk 6,700). Factory policy values are marked as such.
/// </summary>
public static class WageRules
{
    public const decimal MinimumBasic = 6700m;
    public const decimal HouseRentRate = 0.5m;
    public const decimal Medical = 750m;
    public const decimal Transport = 450m;
    public const decimal Food = 1250m;
    public const decimal OvertimeDivisor = 208m;      // 8 hours x 26 working days
    public const decimal OvertimeMultiplier = 2m;     // s.108: twice the ordinary rate
    public const decimal StandardDailyHours = 8m;     // s.100
    public const decimal MaxDailyHours = 10m;         // s.100/s.108 with overtime
    public const decimal MaxWeeklyHours = 60m;        // s.102 with overtime
    public const decimal BreakHours = 1m;             // factory policy: one hour meal break
    public const decimal AttendanceBonus = 500m;      // factory policy
    public const decimal DaysPerMonthForDeduction = 30m;

    public static decimal Gross(decimal basic) =>
        basic + Taka(basic * HouseRentRate) + Medical + Transport + Food;

    public static decimal OvertimeRate(decimal basic) =>
        Math.Round(basic / OvertimeDivisor * OvertimeMultiplier, 2, MidpointRounding.AwayFromZero);

    public static decimal AbsenceDeduction(decimal basic, int daysAbsent) =>
        Taka(basic / DaysPerMonthForDeduction * daysAbsent);

    public static decimal Taka(decimal amount) =>
        Math.Round(amount, 0, MidpointRounding.AwayFromZero);
}
