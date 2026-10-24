using GhostDataAudit.Api.Models.Enums;

namespace GhostDataAudit.Api.Models;

public class DeletionRequest
{
    public string CaseId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public string ReportedStatus { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
    public DateTime AuditAt { get; set; }
    public string RequestText { get; set; } = string.Empty;
}

