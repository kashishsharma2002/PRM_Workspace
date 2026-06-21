namespace Server.Models.Queries;

public class AuditLogQuery
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public long? ActorUserId { get; init; }
    public string? EntityName { get; init; }
    public string? ActionType { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
