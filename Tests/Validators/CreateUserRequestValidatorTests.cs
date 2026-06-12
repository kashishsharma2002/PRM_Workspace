using Server.Common;
using Server.Models.DTOs.Users;
using Tests.Helpers;

namespace Tests;

public class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var result = _validator.Validate(new CreateUserRequestDto
        {
            FullName = MockData.Names.EmployeeA,
            Email = MockData.Email("employee.a"),
            Username = MockData.Username("employee.a"),
            TemporaryPassword = "Welcome1",
            Role = "EMPLOYEE",
            Department = DepartmentConstants.SoftwareDevelopment,
            Designation = DesignationConstants.SoftwareEngineer
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
            Email = MockData.Email("test.user"),
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
            Email = MockData.Email("test.user"),
            Username = "test.user",
            TemporaryPassword = "Welcome1",
            Role = "DIRECTOR"
        });

        Assert.False(result.IsValid);
    }
}
