using Microsoft.Extensions.Configuration;
using ShramikPay.Domain;
using ShramikPay.Services;
using Xunit;

namespace ShramikPay.Tests;

public sealed class PaymentAndExportTests : IDisposable
{
    private readonly TestData _t = new();
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "sp-" + Guid.NewGuid().ToString("N"));
    private readonly IConfiguration _config;

    public PaymentAndExportTests()
    {
        _config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { ["ShramikPay:ExportDir"] = _dir }).Build();
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }

    private WalletPaymentService Wallet(IConfiguration config = null) =>
        new(_t.Store, _t.Crypto, _t.Audit, new NoHttp(), config ?? _config);

    [Fact]
    public async Task Draft_payslips_cannot_be_paid()
    {
        var w = _t.AddWorker();
        var slip = _t.Payroll.Calculate(w.Id, new PayPeriod(2026, 9, 26), "hr");
        await Assert.ThrowsAsync<InvalidOperationException>(() => Wallet().DisburseAsync(slip, "acc"));
    }

    [Fact]
    public async Task Retrying_a_payment_never_pays_twice_SP102_regression()
    {
        var w = _t.AddWorker();
        var slip = _t.Payroll.Calculate(w.Id, new PayPeriod(2026, 9, 26), "hr");
        _t.Payroll.Approve(2026, 9, "acc");
        var first = await Wallet().DisburseAsync(slip, "acc");
        var second = await Wallet().DisburseAsync(slip, "acc");
        Assert.Same(first, second);
        Assert.Single(_t.Store.Payments);
        Assert.Equal(PayslipStatus.Paid, slip.Status);
        Assert.Contains("*******0000", _t.Store.Audit[^1].Details);
        Assert.DoesNotContain("01711000000", string.Join(" ", _t.Store.Audit.Select(a => a.Details)));
    }

    [Fact]
    public void Plain_http_gateway_is_refused()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { ["ShramikPay:WalletGatewayUrl"] = "http://insecure.example" }).Build();
        Assert.Throws<InvalidOperationException>(() => Wallet(cfg));
    }

    [Fact]
    public void Salary_sheet_uses_a_server_generated_name_and_can_be_archived()
    {
        var w = _t.AddWorker();
        w.Name = "=cmd|'/c calc'!A1";
        _t.Payroll.Calculate(w.Id, new PayPeriod(2026, 9, 26), "hr");
        var exporter = new SalarySheetExporter(_t.Store, _t.Audit, _config);
        var path = exporter.Export(2026, 9, "hr");
        Assert.Equal("salary-2026-09.csv", Path.GetFileName(path));
        var lines = File.ReadAllLines(path);
        Assert.Equal(2, lines.Length);
        Assert.Contains("\"'=cmd", lines[1]);
        Assert.True(File.Exists(exporter.Archive(2026, 9)));
    }

    private sealed class NoHttp : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => throw new InvalidOperationException("Live mode is off in tests");
    }
}
