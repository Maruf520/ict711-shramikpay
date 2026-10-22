using ShramikPay.Data;
using ShramikPay.Endpoints;
using ShramikPay.Security;
using ShramikPay.Services;

namespace ShramikPay;

public static class AppFactory
{
    public static WebApplication Create(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton<InMemoryStore>();
        builder.Services.AddSingleton<CryptoService>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<AttendanceService>();
        builder.Services.AddSingleton<PayrollService>();
        builder.Services.AddSingleton<PayslipService>();
        builder.Services.AddSingleton<WalletPaymentService>();
        builder.Services.AddSingleton<SalarySheetExporter>();
        builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

        var app = builder.Build();
        app.UseDeveloperExceptionPage();
        app.UseCors();
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        AuthEndpoints.Map(app);
        WorkerEndpoints.Map(app);
        PayrollEndpoints.Map(app);
        return app;
    }
}
