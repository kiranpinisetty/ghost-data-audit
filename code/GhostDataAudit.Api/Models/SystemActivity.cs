namespace GhostDataAudit.Api.Models;

public class SystemActivity
{
    public string ActivityId { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = string.Empty;
    public string TargetSystem { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string ReportedResult { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

