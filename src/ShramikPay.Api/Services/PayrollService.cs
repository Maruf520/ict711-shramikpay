using ShramikPay.Data;
using ShramikPay.Domain;

namespace ShramikPay.Services;

public class PayrollService
{
    private readonly InMemoryStore _store;
    private readonly AttendanceService _attendance;

    public PayrollService(InMemoryStore store, AttendanceService attendance)
    {
        _store = store;
        _attendance = attendance;
    }

    // Calculates the monthly salary sheet for one worker
    public Payslip Calculate(int workerId, int year, int month, int workingDays, bool includeBonus, bool applyDeductions, string requestedBy)
    {
        var worker = _store.Workers.FirstOrDefault(w => w.Id == workerId);
        var slip = new Payslip { WorkerId = workerId, Year = year, Month = month };
        if (worker != null)
        {
            if (worker.BasicWage > 0)
            {
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
                if (applyDeductions)
                {
                    if (daysPresent < workingDays)
                    {
                        deductions = (workingDays - daysPresent) * (basic / 30);
                    }
                }
                double bonus = 0;
                if (includeBonus)
                {
                    if (daysPresent >= workingDays)
                    {
                        bonus = 500;
                    }
                    else
                    {
                        bonus = 0;
                    }
                }
                slip.Basic = basic;
                slip.Gross = gross;
                slip.OvertimeHours = otHours;
                slip.OvertimePay = otHours * otRate;
                slip.Deductions = deductions;
                slip.NetPay = gross + slip.OvertimePay + bonus - deductions;
            }
            else
            {
                _store.Log.Add("Worker has no basic wage");
            }
        }
        else
        {
            _store.Log.Add("Worker not found");
        }
        _store.Payslips.RemoveAll(p => p.WorkerId == workerId && p.Year == year && p.Month == month);
        _store.Payslips.Add(slip);
        return slip;
    }
}
