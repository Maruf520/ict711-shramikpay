using System.Security.Cryptography;
using ShramikPay.Data;
using ShramikPay.Domain;
using ShramikPay.Security;
using ShramikPay.Services;

namespace ShramikPay.Tests;

/// <summary>Builds an isolated set of services over a fresh in-memory store for each test.</summary>
public sealed class TestData
{
    public InMemoryStore Store { get; } = new();
    public AuditService Audit { get; }
    public AttendanceService Attendance { get; }
    public PayrollService Payroll { get; }
    public PayslipService Payslips { get; }
    public CryptoService Crypto { get; } = new(RandomNumberGenerator.GetBytes(32));
    public User Hr { get; } = new() { Username = "hr.nasrin", Role = "HR" };
    public User Supervisor { get; } = new() { Username = "sup.karim", Role = "Supervisor" };

    public TestData()
    {
        Audit = new AuditService(Store);
        Attendance = new AttendanceService(Store, Audit);
        Payroll = new PayrollService(Store, Attendance, Audit);
        Payslips = new PayslipService(Store, Attendance);
    }

    public Worker AddWorker(decimal basic = 6700m)
    {
        var w = new Worker
        {
            Id = Store.Workers.Count + 1,
            Name = "Rokeya Begum",
            Grade = 5,
            BasicWage = basic,
            NidEncrypted = Crypto.Encrypt("1990123456789"),
            WalletEncrypted = Crypto.Encrypt("01711000000"),
            WalletMasked = CryptoService.Mask("01711000000")
        };
        Store.Workers.Add(w);
        return w;
    }

    /// <summary>Records the same shift on each given day of September 2026.</summary>
    public void Shifts(Worker w, string clockIn, string clockOut, params int[] days)
    {
        foreach (var d in days)
        {
            Attendance.Record(w.Id, $"2026-09-{d:00}", clockIn, clockOut, Supervisor);
        }
    }

    public static readonly int[] September2026WorkingDays =
        { 1, 2, 3, 5, 6, 7, 8, 9, 10, 12, 13, 14, 15, 16, 17, 19, 20, 21, 22, 23, 24, 26, 27, 28, 29, 30 };
}
