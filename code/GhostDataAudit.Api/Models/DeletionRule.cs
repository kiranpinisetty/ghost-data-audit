namespace GhostDataAudit.Api.Models;

public class DeletionRule
{
    public string RuleId { get; set; } = string.Empty;
    public string DataCategory { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string RequiredAction { get; set; } = string.Empty;
}

