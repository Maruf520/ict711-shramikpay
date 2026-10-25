using ShramikPay.Data;
using ShramikPay.Services;
using Xunit;

namespace ShramikPay.Tests;

public class AuthTests
{
    private readonly InMemoryStore _store = new();
    private readonly AuthService _auth;

    public AuthTests()
    {
        _auth = new AuthService(_store);
        _auth.Register("hr.nasrin", "Correct-Horse-9", "HR", null);
    }

    [Fact]
    public void Valid_credentials_return_a_256_bit_random_token()
    {
        var token = _auth.Login("hr.nasrin", "Correct-Horse-9");
        Assert.Equal(64, token.Length);
        Assert.Equal("HR", _auth.GetUser(token).Role);
        Assert.NotEqual(token, _auth.Login("hr.nasrin", "Correct-Horse-9"));
    }

    [Fact]
    public void Wrong_password_or_unknown_user_is_rejected()
    {
        Assert.Null(_auth.Login("hr.nasrin", "wrong-password"));
        Assert.Null(_auth.Login("nobody", "Correct-Horse-9"));
        Assert.Null(_auth.GetUser("not-a-token"));
        Assert.Null(_auth.GetUser(null));
    }

    [Fact]
    public void Passwords_are_salted_pbkdf2_not_md5()
    {
        var a = AuthService.HashPassword("Same-Password-1");
        var b = AuthService.HashPassword("Same-Password-1");
        Assert.NotEqual(a, b);
        Assert.True(AuthService.VerifyPassword("Same-Password-1", a));
        Assert.False(AuthService.VerifyPassword("same-password-1", a));
    }

    [Theory]
    [InlineData("u1", "short", "HR")]
    [InlineData("", "Long-enough-pw", "HR")]
    [InlineData("u2", "Long-enough-pw", "SuperAdmin")]
    [InlineData("hr.nasrin", "Long-enough-pw", "HR")]
    public void Invalid_registrations_are_rejected(string user, string password, string role)
    {
        Assert.Throws<ArgumentException>(() => _auth.Register(user, password, role, null));
    }

    [Fact]
    public void Usernames_are_trimmed_SP102_regression()
    {
        Assert.Equal("acc.rahim", _auth.Register("  acc.rahim ", "Long-enough-pw", "Accounts", null).Username);
    }
}
