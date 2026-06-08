namespace Client.HttpClients;

public class CreateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class CreateUserResponse
{
    public long UserId { get; set; }
    public long EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
}

public class UserListItem
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class UserListResponse
{
    public List<UserListItem> Users { get; set; } = [];
    public int Total { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
}

public class ResetPasswordRequest
{
    public string NewTemporaryPassword { get; set; } = string.Empty;
}
