namespace GhostDataAudit.Api.Models;

public class UserAuditEntry
{
    public string AuditId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
}

