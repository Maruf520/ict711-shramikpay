namespace ShramikPay.Domain;

public class Worker
{
    public int Id;
    public string Name;
    public string Nid;
    public int Grade { get; set; }
    public int BasicWage { get; set; }
    public string WalletNumber;
    public string Line;

    public Worker(int id, string name, string nid, int grade, int basicWage, string walletNumber)
    {
        Id = id;
        Name = name;
        Nid = nid;
        Grade = Grade;
        BasicWage = basicWage;
        WalletNumber = walletNumber;
    }
}

public class AttendanceRecord
{
    public int WorkerId { get; set; }
    public DateTime Date { get; set; }
    public DateTime ClockIn { get; set; }
    public DateTime ClockOut { get; set; }
}

public class User
{
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; }
    public int? WorkerId { get; set; }
}

public class Payslip
{
    public int WorkerId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public double Basic { get; set; }
    public double Gross { get; set; }
    public double OvertimeHours { get; set; }
    public double OvertimePay { get; set; }
    public double Deductions { get; set; }
    public double NetPay { get; set; }
    public bool Paid { get; set; }
}

public class PaymentRecord
{
    public int WorkerId { get; set; }
    public string Reference { get; set; }
    public double Amount { get; set; }
    public DateTime At { get; set; }
}
