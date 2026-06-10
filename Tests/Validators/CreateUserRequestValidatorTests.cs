using Server.Models.DTOs.Users;

namespace Tests;

public class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var result = _validator.Validate(new CreateUserRequestDto
        {
            FullName = "Priya Sharma",
            Email = "priya.sharma@techserve.com",
            Username = "priya.sharma",
            TemporaryPassword = "Welcome1",
            Role = "EMPLOYEE"
        });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("short1")]
    [InlineData("alllowercase1")]
    [InlineData("NoNumbers")]
    public void Validate_InvalidPassword_Fails(string password)
    {
        var result = _validator.Validate(new CreateUserRequestDto
        {
            FullName = "Test User",
            Email = "test@techserve.com",
            Username = "test.user",
            TemporaryPassword = password,
            Role = "EMPLOYEE"
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_InvalidRole_Fails()
    {
        var result = _validator.Validate(new CreateUserRequestDto
        {
            FullName = "Test User",
            Email = "test@techserve.com",
            Username = "test.user",
            TemporaryPassword = "Welcome1",
            Role = "DIRECTOR"
        });

        Assert.False(result.IsValid);
    }
}
