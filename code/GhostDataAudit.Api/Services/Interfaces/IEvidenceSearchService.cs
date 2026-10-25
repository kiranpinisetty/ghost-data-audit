using GhostDataAudit.Api.Models;

namespace GhostDataAudit.Api.Services.Interfaces;

/// <summary>
/// Searches evidence files for records belonging to a resolved identity,
/// returning structured evidence records for the rule engine.
/// </summary>
public interface IEvidenceSearchService
{
    /// <summary>
    /// Initialize the service with the evidence manifest and dataset base path.
    /// Loads and indexes all evidence files.
    /// </summary>
    Task InitializeAsync(IReadOnlyList<EvidenceFile> evidenceManifest, string datasetBasePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search all evidence files captured at or before auditAt for records matching the identity.
    /// </summary>
    List<EvidenceRecord> SearchForUser(ResolvedIdentity identity, DateTime auditAt);
}

