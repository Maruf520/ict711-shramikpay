using System.Security.Cryptography;
using System.Text;

namespace ShramikPay.Security;

public class CryptoService
{
    private static readonly byte[] Key = Encoding.ASCII.GetBytes("Shapla24");

    public string Encrypt(string plain)
    {
        using var des = DES.Create();
        des.Key = Key;
        des.Mode = CipherMode.ECB;
        var bytes = Encoding.UTF8.GetBytes(plain);
        var enc = des.CreateEncryptor().TransformFinalBlock(bytes, 0, bytes.Length);
        return Convert.ToBase64String(enc);
    }

    public string Decrypt(string cipher)
    {
        using var des = DES.Create();
        des.Key = Key;
        des.Mode = CipherMode.ECB;
        var bytes = Convert.FromBase64String(cipher);
        var dec = des.CreateDecryptor().TransformFinalBlock(bytes, 0, bytes.Length);
        return Encoding.UTF8.GetString(dec);
    }
}
