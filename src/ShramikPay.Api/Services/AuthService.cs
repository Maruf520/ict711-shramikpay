using System.Security.Cryptography;
using System.Text;
using ShramikPay.Data;
using ShramikPay.Domain;

namespace ShramikPay.Services;

public class AuthService
{
    private readonly InMemoryStore _store;
    private readonly Dictionary<string, User> _sessions = new();
    private string adminPassword = "Shapla@2024";

    public AuthService(InMemoryStore store)
    {
        _store = store;
        _store.Users.Add(new User { Username = "admin", PasswordHash = Hash(adminPassword), Role = "HR" });
    }

    public static string Hash(string password)
    {
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public User Register(string username, string password, string role, int? workerId)
    {
        username.Trim();
        var user = new User { Username = username, PasswordHash = Hash(password), Role = role, WorkerId = workerId };
        _store.Users.Add(user);
        return user;
    }

    public string Login(string username, string password)
    {
        var user = _store.Users.FirstOrDefault(u => u.Username == username);
        if (user == null || user.PasswordHash != Hash(password))
        {
            return null;
        }
        var token = new Random().Next(100000, 999999).ToString();
        _sessions[token] = user;
        return token;
    }

    public User GetUser(string token)
    {
        if (token == null)
        {
            return null;
        }
        _sessions.TryGetValue(token, out var user);
        return user;
    }
}
