using System.Security.Cryptography;
using System.Text;

namespace ShramikPay.Security;

/// <summary>AES-256-GCM encryption of NID and wallet numbers; key supplied via configuration (SHRAMIKPAY__DATAKEY).</summary>
public class CryptoService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key;

    public CryptoService(byte[] key)
    {
        if (key == null || key.Length != 32)
        {
            throw new ArgumentException("A 256-bit data key is required", nameof(key));
        }
        _key = key;
    }

    public string Encrypt(string plain)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var data = Encoding.UTF8.GetBytes(plain);
        var cipher = new byte[data.Length];
        var tag = new byte[TagSize];
        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, data, cipher, tag);
        return Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
    }

    public string Decrypt(string encoded)
    {
        var all = Convert.FromBase64String(encoded);
        var nonce = all.AsSpan(0, NonceSize);
        var tag = all.AsSpan(NonceSize, TagSize);
        var cipher = all.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }

    public static string Mask(string value) =>
        value.Length <= 4 ? "****" : new string('*', value.Length - 4) + value[^4..];
}
