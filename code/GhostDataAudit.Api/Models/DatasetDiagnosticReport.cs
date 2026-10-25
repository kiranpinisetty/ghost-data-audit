namespace GhostDataAudit.Api.Models;

public class DatasetDiagnosticReport
{
    public int DeletionRequestsCount { get; set; }
    public int SampleDeletionRequestsCount { get; set; }
    public int UserIdentitiesCount { get; set; }
    public int UserAuditLogCount { get; set; }
    public int SystemActivityCount { get; set; }
    public int DeletionRulesCount { get; set; }
    public int EvidenceFilesCount { get; set; }

    public List<string> ValidationErrors { get; set; } = new();
    public List<string> MalformedRecords { get; set; } = new();
    public List<string> MissingReferences { get; set; } = new();
    public List<string> InaccessibleEvidenceFiles { get; set; } = new();

    public bool IsValid =>
        ValidationErrors.Count == 0 &&
        MalformedRecords.Count == 0 &&
        MissingReferences.Count == 0 &&
        InaccessibleEvidenceFiles.Count == 0;
}

