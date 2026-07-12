namespace Client.Models.Audit;

public class AuditLogItem
{
    public long Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public long ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string? ActorRole { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityLabel { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string ActionLabel { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}

public class AuditLogListResponse
{
    public List<AuditLogItem> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
