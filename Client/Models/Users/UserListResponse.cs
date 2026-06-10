namespace Client.Models.Users;

public class UserListResponse
{
    public List<UserListItem> Users { get; set; } = [];
    public int Total { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
}
