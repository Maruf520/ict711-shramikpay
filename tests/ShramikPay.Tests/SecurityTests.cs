using System.Security.Cryptography;
using ShramikPay.Security;
using ShramikPay.Services;
using Xunit;

namespace ShramikPay.Tests;

public class SecurityTests
{
    private readonly TestData _t = new();

    [Fact]
    public void Nid_encryption_round_trips_with_a_fresh_nonce_each_time()
    {
        var a = _t.Crypto.Encrypt("1990123456789");
        var b = _t.Crypto.Encrypt("1990123456789");
        Assert.NotEqual(a, b);
        Assert.Equal("1990123456789", _t.Crypto.Decrypt(a));
    }

    [Fact]
    public void Tampered_ciphertext_is_detected_by_gcm()
    {
        var bytes = Convert.FromBase64String(_t.Crypto.Encrypt("01711000000"));
        bytes[^1] ^= 0x01;
        Assert.Throws<AuthenticationTagMismatchException>(() => _t.Crypto.Decrypt(Convert.ToBase64String(bytes)));
    }

    [Fact]
    public void A_key_that_is_not_256_bits_is_refused()
    {
        Assert.Throws<ArgumentException>(() => new CryptoService(new byte[8]));
    }

    [Fact]
    public void Wallet_numbers_are_masked_for_display_and_logs()
    {
        Assert.Equal("*******0000", CryptoService.Mask("01711000000"));
        Assert.Equal("****", CryptoService.Mask("12"));
    }

    [Theory]
    [InlineData("=HYPERLINK(\"http://x\")", "\"'=HYPERLINK(\"\"http://x\"\")\"")]
    [InlineData("+880", "\"'+880\"")]
    [InlineData("Rokeya, Begum", "\"Rokeya, Begum\"")]
    public void Csv_cells_neutralise_formula_injection(string input, string expected)
    {
        Assert.Equal(expected, SalarySheetExporter.Csv(input));
    }

    [Fact]
    public void Editing_any_audit_entry_breaks_the_hash_chain()
    {
        _t.Audit.Append("hr", "A", "one");
        _t.Audit.Append("hr", "B", "two");
        Assert.True(_t.Audit.VerifyChain());
        _t.Store.Audit[0].Details = "edited";
        Assert.False(_t.Audit.VerifyChain());
    }
}
