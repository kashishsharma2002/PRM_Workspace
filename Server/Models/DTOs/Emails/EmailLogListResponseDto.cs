namespace Server.Models.DTOs.Emails;

public class EmailLogListResponseDto
{
    public List<EmailLogItemDto> Logs { get; set; } = [];
}
