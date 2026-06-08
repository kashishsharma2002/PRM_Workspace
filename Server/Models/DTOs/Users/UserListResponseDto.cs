namespace Server.Models.DTOs.Users;

public class UserListResponseDto
{
    public IReadOnlyList<UserListItemDto> Users { get; set; } = [];
    public int Total { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
}
