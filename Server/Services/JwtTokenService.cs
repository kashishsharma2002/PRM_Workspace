using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Server.Common;
using Server.Models.DTOs.Auth;
using Server.Models.Entities;
using Server.Services.Interfaces;

namespace Server.Services;

public class JwtTokenService(IOptions<JwtSettings> jwtOptions) : IJwtTokenService
{
    private readonly JwtSettings _settings = jwtOptions.Value;

    public LoginResponseDto CreateToken(User user, Employee? employee)
    {
        var expiresAt = DateTime.UtcNow.AddHours(_settings.ExpiryHours);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("role", user.Role),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new("force_password_change", user.ForcePasswordChange.ToString().ToLowerInvariant())
        };

        if (employee is not null)
        {
            claims.Add(new Claim("employee_id", employee.Id.ToString()));

            if (employee.ManagerId.HasValue)
                claims.Add(new Claim("manager_id", employee.ManagerId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new LoginResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt,
            UserId = user.Id,
            EmployeeId = employee?.Id,
            ManagerId = employee?.ManagerId,
            Role = user.Role,
            FullName = user.FullName,
            ForcePasswordChange = user.ForcePasswordChange
        };
    }
}
