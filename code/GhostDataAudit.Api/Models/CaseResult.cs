namespace GhostDataAudit.Api.Models;

public class CaseResult
{
    public string CaseId { get; set; } = string.Empty;
    public string DeletionStatus { get; set; } = string.Empty;
    public List<AuditStep> Steps { get; set; } = new();
}

