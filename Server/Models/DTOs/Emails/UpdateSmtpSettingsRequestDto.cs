namespace Server.Models.DTOs.Emails;

public class UpdateSmtpSettingsRequestDto
{
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public bool? SmtpSslEnabled { get; set; }
    public string? SmtpFromEmail { get; set; }
    public string? SmtpFromName { get; set; }
}
