using ShramikPay.Data;
using ShramikPay.Domain;
using ShramikPay.Security;

namespace ShramikPay.Services;

public class WalletPaymentService
{
    private readonly InMemoryStore _store;
    private readonly CryptoService _crypto;
    private readonly AuditService _audit;
    private readonly IHttpClientFactory _http;
    private readonly Uri _gateway;
    private readonly bool _liveMode;

    public WalletPaymentService(InMemoryStore store, CryptoService crypto, AuditService audit, IHttpClientFactory http, IConfiguration config)
    {
        _store = store;
        _crypto = crypto;
        _audit = audit;
        _http = http;
        _gateway = new Uri(config["ShramikPay:WalletGatewayUrl"] ?? "https://api.walletgateway.example.com.bd/disburse");
        _liveMode = config.GetValue<bool>("ShramikPay:WalletLiveMode");
        if (_gateway.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Wallet gateway must use HTTPS");
        }
    }

    public async Task<PaymentRecord> DisburseAsync(Payslip slip, string paidBy)
    {
        var reference = $"SP-{slip.Year}{slip.Month:00}-{slip.WorkerId}";
        var existing = _store.Payments.Find(p => p.Reference == reference);
        if (existing != null)
        {
            return existing; // idempotent: a retry never pays twice
        }
        if (slip.Status != PayslipStatus.Approved)
        {
            throw new InvalidOperationException("Only approved payslips can be paid");
        }
        var worker = _store.Workers.Find(w => w.Id == slip.WorkerId) ?? throw new KeyNotFoundException("Worker not found");
        if (_liveMode)
        {
            var client = _http.CreateClient("wallet");
            var body = new { wallet = _crypto.Decrypt(worker.WalletEncrypted), amount = slip.NetPay, reference };
            using var response = await client.PostAsJsonAsync(_gateway, body);
            response.EnsureSuccessStatusCode();
        }
        var payment = new PaymentRecord { WorkerId = worker.Id, Reference = reference, Amount = slip.NetPay, At = DateTime.UtcNow };
        _store.Payments.Add(payment);
        slip.Status = PayslipStatus.Paid;
        _audit.Append(paidBy, "WagePaid", $"{reference} Tk {slip.NetPay} to wallet {worker.WalletMasked}");
        return payment;
    }
}
