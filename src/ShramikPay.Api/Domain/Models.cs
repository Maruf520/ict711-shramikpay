namespace ShramikPay.Domain;

public class Worker
{
    public int Id { get; init; }
    public string Name { get; set; }
    public string NidEncrypted { get; set; }
    public int Grade { get; set; }
    public decimal BasicWage { get; set; }
    public string WalletEncrypted { get; set; }
    public string WalletMasked { get; set; }
}

public class AttendanceRecord
{
    public int WorkerId { get; set; }
    public DateOnly Date { get; set; }
    public DateTime ClockIn { get; set; }
    public DateTime ClockOut { get; set; }
    public string RecordedBy { get; set; }
}

public class User
{
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; }
    public int? WorkerId { get; set; }
}

public enum PayslipStatus
{
    Draft,
    Approved,
    Paid
}

public class Payslip
{
    public int WorkerId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Basic { get; set; }
    public decimal Gross { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal AttendanceBonus { get; set; }
    public decimal Deductions { get; set; }
    public decimal NetPay { get; set; }
    public PayslipStatus Status { get; set; }
    public List<string> ComplianceAlerts { get; } = new();
}

public class PaymentRecord
{
    public int WorkerId { get; set; }
    public string Reference { get; set; }
    public decimal Amount { get; set; }
    public DateTime At { get; set; }
}

public class AuditEntry
{
    public int Sequence { get; set; }
    public DateTime At { get; set; }
    public string User { get; set; }
    public string Action { get; set; }
    public string Details { get; set; }
    public string PreviousHash { get; set; }
    public string Hash { get; set; }
}

public record PayPeriod(int Year, int Month, int WorkingDays);
