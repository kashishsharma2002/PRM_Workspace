using Server.Common;

namespace Server.Models.Entities;

public class ResourceProfile
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long? ManagerId { get; set; }
    public string ResourceStatus { get; set; } = ResourceStatusConstants.Bench;
    public bool IsTimesheetFrozen { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
