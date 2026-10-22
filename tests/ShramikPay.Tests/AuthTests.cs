using ShramikPay.Data;
using ShramikPay.Services;
using Xunit;

namespace ShramikPay.Tests;

public class AuthTests
{
    [Fact]
    public void Admin_can_log_in()
    {
        var auth = new AuthService(new InMemoryStore());
        Assert.NotNull(auth.Login("admin", "Shapla@2024"));
    }

    [Fact]
    public void Wrong_password_is_rejected()
    {
        var auth = new AuthService(new InMemoryStore());
        Assert.Null(auth.Login("admin", "wrong"));
    }
}
