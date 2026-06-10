namespace Client.Models.Users;

public class CreateUserResponse
{
    public long UserId { get; set; }
    public long EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
}
