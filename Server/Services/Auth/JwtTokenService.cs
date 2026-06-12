using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Server.Common;
using Server.Models.DTOs.Auth;
using Server.Models.Entities;

namespace Server.Services.Auth;

public class JwtTokenService(IOptions<JwtSettings> jwtOptions) : IJwtTokenService
{
    private readonly JwtSettings _settings = jwtOptions.Value;

    public LoginResponseDto CreateToken(User user, string primaryRole, ResourceProfile? resourceProfile)
    {
        var expiresAt = DateTime.UtcNow.AddHours(_settings.ExpiryHours);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("role", primaryRole),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new("force_password_change", user.IsTemporaryPassword.ToString().ToLowerInvariant())
        };

        if (resourceProfile is not null)
        {
            claims.Add(new Claim("employee_id", resourceProfile.Id.ToString()));

            if (resourceProfile.ManagerId.HasValue)
                claims.Add(new Claim("manager_id", resourceProfile.ManagerId.Value.ToString()));
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
            EmployeeId = resourceProfile?.Id,
            ManagerId = resourceProfile?.ManagerId,
            Role = primaryRole,
            FullName = user.FullName,
            ForcePasswordChange = user.IsTemporaryPassword
        };
    }
}
