namespace Server.Models.DTOs.Emails;

public class EmailOperationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
