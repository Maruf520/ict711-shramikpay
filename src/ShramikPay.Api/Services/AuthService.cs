using System.Security.Cryptography;
using ShramikPay.Data;
using ShramikPay.Domain;

namespace ShramikPay.Services;

public class AuthService
{
    public static readonly IReadOnlySet<string> Roles = new HashSet<string> { "HR", "Supervisor", "Accounts", "Auditor", "Worker" };
    private const int Iterations = 210_000;
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);
    private readonly InMemoryStore _store;
    private readonly Dictionary<string, (User User, DateTime Expires)> _sessions = new();

    public AuthService(InMemoryStore store)
    {
        _store = store;
    }

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
    }

    public static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split(':');
        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public User Register(string username, string password, string role, int? workerId)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password) || password.Length < 10)
        {
            throw new ArgumentException("Username and a password of at least 10 characters are required");
        }
        if (!Roles.Contains(role))
        {
            throw new ArgumentException("Unknown role");
        }
        username = username.Trim();
        if (_store.Users.Exists(u => u.Username == username))
        {
            throw new ArgumentException("Username already exists");
        }
        var user = new User { Username = username, PasswordHash = HashPassword(password), Role = role, WorkerId = workerId };
        _store.Users.Add(user);
        return user;
    }

    public string Login(string username, string password)
    {
        var user = _store.Users.Find(u => u.Username == username);
        if (user == null || !VerifyPassword(password, user.PasswordHash))
        {
            return null;
        }
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        lock (_sessions)
        {
            _sessions[token] = (user, DateTime.UtcNow.Add(SessionLifetime));
        }
        return token;
    }

    public User GetUser(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }
        lock (_sessions)
        {
            if (_sessions.TryGetValue(token, out var session) && session.Expires > DateTime.UtcNow)
            {
                return session.User;
            }
            _sessions.Remove(token);
            return null;
        }
    }
}
