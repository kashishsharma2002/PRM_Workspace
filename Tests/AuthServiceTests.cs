using Server.Common;

namespace Tests;

public class PasswordValidatorTests
{
    [Theory]
    [InlineData("Short1")]
    [InlineData("alllowercase1")]
    [InlineData("NoNumbers")]
    public void IsValid_RejectsInvalidPasswords(string password)
    {
        var isValid = PasswordValidator.IsValid(password, out _);
        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_AcceptsValidPassword()
    {
        var isValid = PasswordValidator.IsValid("Admin@1234", out var error);
        Assert.True(isValid);
        Assert.Empty(error);
    }
}
