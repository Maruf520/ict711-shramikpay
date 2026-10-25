using ShramikPay.Data;
using ShramikPay.Domain;

namespace ShramikPay.Services;

public class PayrollService
{
    private readonly InMemoryStore _store;
    private readonly AttendanceService _attendance;
    private readonly AuditService _audit;

    public PayrollService(InMemoryStore store, AttendanceService attendance, AuditService audit)
    {
        _store = store;
        _attendance = attendance;
        _audit = audit;
    }

    public Payslip Calculate(int workerId, PayPeriod period, string requestedBy)
    {
        if (_store.LockedPeriods.Contains((period.Year, period.Month)))
        {
            throw new InvalidOperationException("Payroll for this month is already approved");
        }
        var worker = _store.Workers.Find(w => w.Id == workerId) ?? throw new KeyNotFoundException("Worker not found");
        if (worker.BasicWage < WageRules.MinimumBasic)
        {
            throw new InvalidOperationException($"Basic wage {worker.BasicWage} is below the legal minimum {WageRules.MinimumBasic}");
        }
        var summary = PayCalculator.Summarise(_attendance.ForMonth(workerId, period.Year, period.Month));
        var slip = PayCalculator.Build(worker, period, summary);
        _store.Payslips.RemoveAll(p => p.WorkerId == workerId && p.Year == period.Year && p.Month == period.Month);
        _store.Payslips.Add(slip);
        _audit.Append(requestedBy, "PayslipCalculated", $"worker {workerId} {period.Year}-{period.Month:00} net {slip.NetPay}");
        return slip;
    }

    public List<Payslip> Run(PayPeriod period, string requestedBy) =>
        _store.Workers.Select(w => Calculate(w.Id, period, requestedBy)).ToList();

    public int Approve(int year, int month, string approvedBy)
    {
        var slips = _store.Payslips.Where(p => p.Year == year && p.Month == month).ToList();
        if (slips.Count == 0)
        {
            throw new InvalidOperationException("No payroll has been calculated for this month");
        }
        slips.ForEach(p => p.Status = PayslipStatus.Approved);
        _store.LockedPeriods.Add((year, month));
        _audit.Append(approvedBy, "PayrollApproved", $"{year}-{month:00} {slips.Count} payslips, total {slips.Sum(p => p.NetPay)}");
        return slips.Count;
    }
}
