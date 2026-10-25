using System.Security.Cryptography;
using System.Text;
using ShramikPay.Data;
using ShramikPay.Domain;

namespace ShramikPay.Services;

/// <summary>Append-only, hash-chained audit trail: any edit or deletion of an entry breaks the chain.</summary>
public class AuditService
{
    private readonly InMemoryStore _store;
    private readonly object _gate = new();

    public AuditService(InMemoryStore store)
    {
        _store = store;
    }

    public AuditEntry Append(string user, string action, string details)
    {
        lock (_gate)
        {
            var previous = _store.Audit.Count == 0 ? new string('0', 64) : _store.Audit[^1].Hash;
            var entry = new AuditEntry
            {
                Sequence = _store.Audit.Count + 1,
                At = DateTime.UtcNow,
                User = user,
                Action = action,
                Details = details,
                PreviousHash = previous
            };
            entry.Hash = ComputeHash(entry);
            _store.Audit.Add(entry);
            return entry;
        }
    }

    public bool VerifyChain()
    {
        var previous = new string('0', 64);
        foreach (var entry in _store.Audit)
        {
            if (entry.PreviousHash != previous || entry.Hash != ComputeHash(entry))
            {
                return false;
            }
            previous = entry.Hash;
        }
        return true;
    }

    private static string ComputeHash(AuditEntry e)
    {
        var text = string.Join('|', e.Sequence, e.At.ToString("O"), e.User, e.Action, e.Details, e.PreviousHash);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }
}
