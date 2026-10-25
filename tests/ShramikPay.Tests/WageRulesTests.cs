using ShramikPay.Services;
using Xunit;

namespace ShramikPay.Tests;

public class WageRulesTests
{
    [Fact]
    public void Gross_for_minimum_basic_is_the_2023_RMG_minimum_of_12500()
    {
        Assert.Equal(12500m, WageRules.Gross(6700m));
    }

    [Theory]
    [InlineData(6700, 64.42)]
    [InlineData(8000, 76.92)]
    [InlineData(10400, 100.00)]
    public void Overtime_rate_is_twice_basic_over_208_without_integer_truncation(int basic, double expected)
    {
        Assert.Equal((decimal)expected, WageRules.OvertimeRate(basic));
    }

    [Fact]
    public void Absence_deduction_is_basic_over_30_per_day_rounded_to_whole_taka()
    {
        Assert.Equal(447m, WageRules.AbsenceDeduction(6700m, 2));
        Assert.Equal(0m, WageRules.AbsenceDeduction(6700m, 0));
    }

    [Fact]
    public void Taka_rounds_half_away_from_zero()
    {
        Assert.Equal(193m, WageRules.Taka(193.26m));
        Assert.Equal(194m, WageRules.Taka(193.5m));
    }
}
