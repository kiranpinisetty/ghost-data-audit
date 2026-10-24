using GhostDataAudit.Api.Models;

namespace GhostDataAudit.Api.Services.Interfaces;

public interface IAuditOrchestratorService
{
    Task<CaseResult> AuditCaseAsync(DeletionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CaseResult>> AuditAllAsync(CancellationToken cancellationToken = default);
}

