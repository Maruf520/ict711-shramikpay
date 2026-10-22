using ShramikPay.Data;
using ShramikPay.Domain;
using ShramikPay.Services;

namespace ShramikPay.Tests;

public static class TestData
{
    public static (InMemoryStore store, AttendanceService attendance, PayrollService payroll) Payroll()
    {
        var store = new InMemoryStore();
        var attendance = new AttendanceService(store);
        return (store, attendance, new PayrollService(store, attendance));
    }

    public static Worker AddWorker(InMemoryStore store, int basic = 6700)
    {
        var w = new Worker(store.Workers.Count + 1, "Rokeya Begum", "1990123456789", 5, basic, "01711000000");
        store.Workers.Add(w);
        return w;
    }
}
