using ShramikPay.Domain;

namespace ShramikPay.Data;

public class InMemoryStore
{
    public List<Worker> Workers { get; } = new();
    public List<AttendanceRecord> Attendance { get; } = new();
    public List<User> Users { get; } = new();
    public List<Payslip> Payslips { get; } = new();
    public List<PaymentRecord> Payments { get; } = new();
    public List<AuditEntry> Audit { get; } = new();
    public HashSet<(int Year, int Month)> LockedPeriods { get; } = new();
}
