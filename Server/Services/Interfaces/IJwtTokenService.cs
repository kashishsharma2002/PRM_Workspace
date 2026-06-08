using Server.Models.DTOs.Auth;
using Server.Models.Entities;

namespace Server.Services.Interfaces;

public interface IJwtTokenService
{
    LoginResponseDto CreateToken(User user, Employee? employee);
}
