namespace Client.Models.Timesheets;

public class ActivityTagItem
{
    public long Id { get; set; }
    public string TagCode { get; set; } = string.Empty;
    public string TagName { get; set; } = string.Empty;
    public string? TagCategory { get; set; }
}
