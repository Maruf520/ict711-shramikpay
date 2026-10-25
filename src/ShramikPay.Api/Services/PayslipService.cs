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

    public Payslip Get(int workerId, int year, int month) =>
        _store.Payslips.Find(p => p.WorkerId == workerId && p.Year == year && p.Month == month);

    // Estimated take-home pay shown to the worker on the kiosk before payroll is run
    public decimal Preview(Worker worker, PayPeriod period)
    {
        var summary = PayCalculator.Summarise(_attendance.ForMonth(worker.Id, period.Year, period.Month));
        return PayCalculator.Build(worker, period, summary).NetPay;
    }
}
