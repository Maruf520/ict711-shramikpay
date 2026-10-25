using System.Security.Cryptography;
using ShramikPay.Data;
using ShramikPay.Endpoints;
using ShramikPay.Infrastructure;
using ShramikPay.Security;
using ShramikPay.Services;

namespace ShramikPay;

public static class AppFactory
{
    public static WebApplication Create(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var config = builder.Configuration;
        var isDev = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing");
        var key = config["ShramikPay:DataKey"] is string k ? Convert.FromBase64String(k)
            : isDev ? RandomNumberGenerator.GetBytes(32)
            : throw new InvalidOperationException("ShramikPay:DataKey must be configured");

        builder.Services.AddSingleton<InMemoryStore>();
        builder.Services.AddSingleton(new CryptoService(key));
        builder.Services.AddSingleton<AuditService>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<AttendanceService>();
        builder.Services.AddSingleton<PayrollService>();
        builder.Services.AddSingleton<PayslipService>();
        builder.Services.AddSingleton<WalletPaymentService>();
        builder.Services.AddSingleton<SalarySheetExporter>();
        builder.Services.AddHttpClient("wallet");
        var origins = config.GetSection("ShramikPay:AllowedOrigins").Get<string[]>() ?? new[] { "https://hr.shapla-apparels.example" };
        builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origins).WithHeaders("Authorization", "Content-Type").WithMethods("GET", "POST")));

        var app = builder.Build();
        app.UseMiddleware<ErrorHandling>();
        if (!isDev)
        {
            app.UseHsts();
        }
        app.UseCors();
        var adminPassword = config["ShramikPay:AdminPassword"];
        if (!string.IsNullOrEmpty(adminPassword))
        {
            app.Services.GetRequiredService<AuthService>().Register("admin", adminPassword, "HR", null);
        }
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        AuthEndpoints.Map(app);
        WorkerEndpoints.Map(app);
        PayrollEndpoints.Map(app);
        return app;
    }
}
