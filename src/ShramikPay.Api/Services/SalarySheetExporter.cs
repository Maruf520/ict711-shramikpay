using System.Globalization;
using System.IO.Compression;
using System.Text;
using ShramikPay.Data;

namespace ShramikPay.Services;

public class SalarySheetExporter
{
    private readonly InMemoryStore _store;
    private readonly AuditService _audit;
    private readonly string _exportDir;

    public SalarySheetExporter(InMemoryStore store, AuditService audit, IConfiguration config)
    {
        _store = store;
        _audit = audit;
        _exportDir = config["ShramikPay:ExportDir"] ?? "exports";
    }

    public string Export(int year, int month, string exportedBy)
    {
        var sb = new StringBuilder();
        sb.AppendLine("WorkerId,Name,Gross,OvertimePay,Bonus,Deductions,NetPay,Status");
        foreach (var p in _store.Payslips.Where(p => p.Year == year && p.Month == month).OrderBy(p => p.WorkerId))
        {
            var name = _store.Workers.Find(x => x.Id == p.WorkerId)?.Name ?? "";
            sb.AppendLine(string.Join(',', p.WorkerId.ToString(CultureInfo.InvariantCulture), Csv(name),
                Num(p.Gross), Num(p.OvertimePay), Num(p.AttendanceBonus), Num(p.Deductions), Num(p.NetPay), p.Status));
        }
        Directory.CreateDirectory(_exportDir);
        var path = Path.Combine(_exportDir, $"salary-{year:0000}-{month:00}.csv");
        File.WriteAllText(path, sb.ToString());
        _audit.Append(exportedBy, "SalarySheetExported", Path.GetFileName(path));
        return path;
    }

    public string Archive(int year, int month)
    {
        var csv = Path.Combine(_exportDir, $"salary-{year:0000}-{month:00}.csv");
        var zip = Path.ChangeExtension(csv, ".zip");
        File.Delete(zip);
        using var archive = ZipFile.Open(zip, ZipArchiveMode.Create);
        archive.CreateEntryFromFile(csv, Path.GetFileName(csv));
        return zip;
    }

    // Neutralises spreadsheet formula injection and quotes every text field
    public static string Csv(string value)
    {
        var v = value ?? "";
        if (v.Length > 0 && "=+-@\t\r".Contains(v[0]))
        {
            v = "'" + v;
        }
        return "\"" + v.Replace("\"", "\"\"") + "\"";
    }

    private static string Num(decimal d) => d.ToString("0.##", CultureInfo.InvariantCulture);
}
