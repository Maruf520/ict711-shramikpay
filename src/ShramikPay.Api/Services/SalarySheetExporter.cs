using System.Diagnostics;
using System.Text;
using ShramikPay.Data;

namespace ShramikPay.Services;

public class SalarySheetExporter
{
    private readonly InMemoryStore _store;

    public SalarySheetExporter(InMemoryStore store)
    {
        _store = store;
    }

    public string Export(int year, int month, string fileName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("WorkerId,Name,Gross,OvertimePay,Deductions,NetPay");
        foreach (var p in _store.Payslips.Where(p => p.Year == year && p.Month == month))
        {
            var w = _store.Workers.First(x => x.Id == p.WorkerId);
            sb.AppendLine(p.WorkerId + "," + w.Name + "," + p.Gross + "," + p.OvertimePay + "," + p.Deductions + "," + p.NetPay);
        }
        Directory.CreateDirectory("exports");
        var path = Path.Combine("exports", fileName);
        File.WriteAllText(path, sb.ToString());
        return path;
    }

    public void Archive(string fileName)
    {
        Process.Start("zip", "exports/archive.zip exports/" + fileName);
    }
}
