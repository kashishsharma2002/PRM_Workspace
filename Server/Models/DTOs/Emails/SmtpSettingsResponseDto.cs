namespace Server.Models.DTOs.Emails;

public class SmtpSettingsResponseDto
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPasswordMasked { get; set; } = string.Empty;
    public bool SmtpSslEnabled { get; set; }
    public string SmtpFromEmail { get; set; } = string.Empty;
    public string SmtpFromName { get; set; } = string.Empty;
}
