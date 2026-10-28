using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ShramikPay.Services;
using Xunit;

namespace ShramikPay.Tests;

/// <summary>End-to-end tests against the real ASP.NET Core pipeline on a random local port.</summary>
public sealed class ApiTests : IAsyncLifetime
{
    private static readonly string[] TestHostArgs = { "--urls", "http://127.0.0.1:0", "--environment", "Testing" };
    private WebApplication _app;
    private HttpClient _client;

    public async Task InitializeAsync()
    {
        _app = AppFactory.Create(TestHostArgs);
        var auth = _app.Services.GetRequiredService<AuthService>();
        auth.Register("hr.nasrin", "Hr-Password-01", "HR", null);
        auth.Register("sup.karim", "Sup-Password-01", "Supervisor", null);
        auth.Register("acc.rahim", "Acc-Password-01", "Accounts", null);
        auth.Register("aud.sultana", "Aud-Password-01", "Auditor", null);
        auth.Register("worker.rokeya", "Wkr-Password-01", "Worker", 1);
        auth.Register("worker.jamal", "Wkr-Password-02", "Worker", 2);
        await _app.StartAsync();
        _client = new HttpClient { BaseAddress = new Uri(_app.Urls.First()) };
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    private async Task<HttpClient> As(string user, string password)
    {
        var res = await _client.PostAsJsonAsync("/api/auth/login", new { username = user, password });
        var token = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        var c = new HttpClient { BaseAddress = _client.BaseAddress };
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    [Fact]
    public async Task Full_payroll_cycle_from_attendance_to_wallet_payment()
    {
        var hr = await As("hr.nasrin", "Hr-Password-01");
        var sup = await As("sup.karim", "Sup-Password-01");
        var acc = await As("acc.rahim", "Acc-Password-01");
        var worker = await As("worker.rokeya", "Wkr-Password-01");

        var created = await hr.PostAsJsonAsync("/api/workers", new { name = "Rokeya Begum", nid = "1990123456789", grade = 5, basicWage = 6700, walletNumber = "01711000000" });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await hr.PostAsJsonAsync("/api/users", new { username = "line.chief", password = "Line-Chief-01", role = "Supervisor" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await sup.PostAsJsonAsync("/api/attendance", new { workerId = 1, date = "2026-09-01", clockIn = "08:00", clockOut = "19:00" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await hr.PostAsJsonAsync("/api/payroll/run", new { year = 2026, month = 9, workingDays = 26 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await acc.PostAsync("/api/payroll/2026/9/approve", null)).StatusCode);

        var slip = await worker.GetFromJsonAsync<JsonElement>("/api/payslips/1/2026/9");
        Assert.Equal(2m, slip.GetProperty("overtimeHours").GetDecimal());

        var pay = await acc.PostAsync("/api/payments/1/2026/9", null);
        Assert.Equal(HttpStatusCode.OK, pay.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await hr.GetAsync("/api/exports/salary-sheet/2026/9")).StatusCode);

        var audit = await (await As("aud.sultana", "Aud-Password-01")).GetFromJsonAsync<JsonElement>("/api/audit/verify");
        Assert.True(audit.GetProperty("intact").GetBoolean());
    }

    [Fact]
    public async Task Requests_without_a_valid_token_get_401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/payslips/1/2026/9")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostAsJsonAsync("/api/auth/login", new { username = "hr.nasrin", password = "nope" })).StatusCode);
    }

    [Fact]
    public async Task Wrong_role_gets_403_not_a_500_crash_SP101_regression()
    {
        var sup = await As("sup.karim", "Sup-Password-01");
        var res = await sup.PostAsJsonAsync("/api/payroll/run", new { year = 2026, month = 9, workingDays = 26 });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Worker_cannot_read_another_workers_payslip_SP101_regression()
    {
        var jamal = await As("worker.jamal", "Wkr-Password-02");
        Assert.Equal(HttpStatusCode.Forbidden, (await jamal.GetAsync("/api/payslips/1/2026/9")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await jamal.GetAsync("/api/payslips/2/2026/9")).StatusCode);
    }

    [Fact]
    public async Task Invalid_input_returns_400_without_internal_details()
    {
        var hr = await As("hr.nasrin", "Hr-Password-01");
        var badNid = await hr.PostAsJsonAsync("/api/workers", new { name = "X", nid = "12345", grade = 5, basicWage = 6700, walletNumber = "01711000000" });
        Assert.Equal(HttpStatusCode.BadRequest, badNid.StatusCode);
        var lowWage = await hr.PostAsJsonAsync("/api/workers", new { name = "X", nid = "1990123456789", grade = 5, basicWage = 5000, walletNumber = "01711000000" });
        Assert.Equal(HttpStatusCode.BadRequest, lowWage.StatusCode);
        var badRole = await hr.PostAsJsonAsync("/api/users", new { username = "x", password = "Long-enough-1", role = "Root" });
        Assert.Equal(HttpStatusCode.BadRequest, badRole.StatusCode);
        var unknown = await (await As("sup.karim", "Sup-Password-01")).PostAsJsonAsync("/api/attendance", new { workerId = 77, date = "2026-09-01", clockIn = "08:00", clockOut = "17:00" });
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.DoesNotContain("   at ", await unknown.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Health_endpoint_and_conflict_mapping()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health")).StatusCode);
        var acc = await As("acc.rahim", "Acc-Password-01");
        Assert.Equal(HttpStatusCode.Conflict, (await acc.PostAsync("/api/payroll/2026/9/approve", null)).StatusCode);
    }
}
