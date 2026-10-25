using GhostDataAudit.Api.Models;

namespace GhostDataAudit.Api.Services.Interfaces;

public interface IAuditOrchestratorService
{
    Task InitializeDatasetAsync(string datasetDir, CancellationToken cancellationToken = default);
    Task<CaseResult> AuditCaseAsync(DeletionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CaseResult>> AuditAllAsync(CancellationToken cancellationToken = default);
}

