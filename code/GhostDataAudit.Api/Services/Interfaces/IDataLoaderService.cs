using GhostDataAudit.Api.Models;

namespace GhostDataAudit.Api.Services.Interfaces;

public interface IDataLoaderService
{
    Task<IReadOnlyList<DeletionRequest>> LoadDeletionRequestsAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserIdentity>> LoadUserIdentitiesAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserAuditEntry>> LoadUserAuditLogAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemActivity>> LoadSystemActivityAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeletionRule>> LoadDeletionRulesAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EvidenceFile>> LoadEvidenceFilesAsync(string filePath, CancellationToken cancellationToken = default);

    string ResolveSafeEvidencePath(string baseDatasetDirectory, string relativeEvidencePath);
    Task<DatasetDiagnosticReport> ValidateAndDiagnoseDatasetAsync(string datasetDirectory, CancellationToken cancellationToken = default);
}
