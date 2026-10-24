namespace GhostDataAudit.Api.Models;

public class EvidenceFile
{
    public string FileId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }
}

