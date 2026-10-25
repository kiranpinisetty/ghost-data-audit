namespace GhostDataAudit.Api.Models;

/// <summary>
/// A single piece of evidence found during the audit. Represents one record
/// in one system that is relevant to the deletion subject.
/// </summary>
public class EvidenceRecord
{
    /// <summary>The evidence file_id from evidence_files.csv that produced this record.</summary>
    public string EvidenceFileId { get; set; } = string.Empty;

    /// <summary>The system owning this record (account_service, search_service, etc.).</summary>
    public string SystemId { get; set; } = string.Empty;

    /// <summary>The specific resource (account_005, search_1874, etc.).</summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>The deletion-rules category this record maps to.</summary>
    public string DataCategory { get; set; } = string.Empty;

    /// <summary>When the evidence was captured.</summary>
    public DateTime CapturedAt { get; set; }

    /// <summary>Whether the record still contains direct identifiers (PII).</summary>
    public bool HasDirectIdentifiers { get; set; }

    /// <summary>Whether the record has been anonymized (identifiers removed, record present).</summary>
    public bool IsAnonymized { get; set; }

    /// <summary>Whether the record has been redacted (some identifiers removed per policy).</summary>
    public bool IsRedacted { get; set; }

    /// <summary>For connector_subscriptions: whether the data flow is enabled.</summary>
    public bool? IsEnabled { get; set; }

    /// <summary>For backup_restore_entries: whether restore is allowed.</summary>
    public bool? RestoreAllowed { get; set; }

    /// <summary>For security_audit_records: the retention-until timestamp.</summary>
    public DateTime? RetentionUntil { get; set; }

    /// <summary>The customer_reference (user_id) in this record, if present.</summary>
    public string? CustomerReference { get; set; }

    /// <summary>Source origin type of the evidence.</summary>
    public string SourceType { get; set; } = string.Empty;
}

