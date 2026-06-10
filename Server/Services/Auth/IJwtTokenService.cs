using Server.Models.DTOs.Auth;
using Server.Models.Entities;

namespace Server.Services.Auth;

public interface IJwtTokenService
{
    LoginResponseDto CreateToken(User user, Employee? employee);
}
