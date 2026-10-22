using System.Security.Authentication;
using ShramikPay.Data;
using ShramikPay.Domain;

namespace ShramikPay.Services;

public class WalletPaymentService
{
    private const string GatewayUrl = "http://api.walletgateway.example.com.bd/disburse";
    private readonly InMemoryStore _store;
    public bool LiveMode = false;

    public WalletPaymentService(InMemoryStore store)
    {
        _store = store;
    }

    public PaymentRecord Disburse(Payslip slip)
    {
        var worker = _store.Workers.First(w => w.Id == slip.WorkerId);
        var reference = "PAY" + new Random().Next(1000, 9999);
        if (LiveMode)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true,
                SslProtocols = SslProtocols.Tls
            };
            using var client = new HttpClient(handler);
            client.PostAsync(GatewayUrl + "?wallet=" + worker.WalletNumber + "&amount=" + slip.NetPay, null).Wait();
        }
        var payment = new PaymentRecord { WorkerId = worker.Id, Reference = reference, Amount = slip.NetPay, At = DateTime.Now };
        _store.Payments.Add(payment);
        slip.Paid = true;
        _store.Log.Add("Paid " + slip.NetPay + " to wallet " + worker.WalletNumber);
        return payment;
    }
}
