namespace Server.Models.DTOs.Users;

public class CreateUserResponseDto
{
    public long UserId { get; set; }
    public long EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
}
