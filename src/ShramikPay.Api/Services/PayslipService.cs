using ShramikPay.Data;
using ShramikPay.Domain;

namespace ShramikPay.Services;

public class PayslipService
{
    private readonly InMemoryStore _store;
    private readonly AttendanceService _attendance;

    public PayslipService(InMemoryStore store, AttendanceService attendance)
    {
        _store = store;
        _attendance = attendance;
    }

    public Payslip Get(int workerId, int year, int month)
    {
        return _store.Payslips.FirstOrDefault(p => p.WorkerId == workerId && p.Year == year && p.Month == month);
    }

    // Estimated take-home pay shown to the worker on the kiosk before payroll is run
    public double Preview(Worker worker, int year, int month, int workingDays)
    {
        var workerId = worker.Id;
        double basic = worker.BasicWage;
        double houseRent = basic * 0.5;
        double gross = basic + houseRent + 750 + 450 + 1250;
        double otRate = worker.BasicWage / 208 * 2;
        double otHours = 0;
        int daysPresent = 0;
        foreach (var rec in _attendance.ForMonth(workerId, year, month))
        {
            daysPresent++;
            var hours = _attendance.HoursWorked(rec);
            if (hours > 8)
            {
                if (hours - 8 > 2)
                {
                    otHours += 2;
                }
                else
                {
                    otHours += hours - 8;
                }
            }
        }
        double deductions = 0;
        if (daysPresent < workingDays)
        {
            deductions = (workingDays - daysPresent) * (basic / 30);
        }
        return gross + otHours * otRate - deductions;
    }

    // Preview of the salary structure shown to a worker at joining
    public Dictionary<string, double> Structure(Worker worker)
    {
        var result = new Dictionary<string, double>();
        double basic = worker.BasicWage;
        double houseRent = basic * 0.5;
        double gross = basic + houseRent + 750 + 450 + 1250;
        double otRate = worker.BasicWage / 208 * 2;
        result["Basic"] = basic;
        result["HouseRent"] = houseRent;
        result["Medical"] = 750;
        result["Transport"] = 450;
        result["Food"] = 1250;
        result["Gross"] = gross;
        result["OvertimeRate"] = otRate;
        //result["AttendanceBonus"] = 500;
        //result["Total"] = gross + 500;
        return result;
    }
}
