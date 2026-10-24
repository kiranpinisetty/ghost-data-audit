using GhostDataAudit.Api.Models;
using GhostDataAudit.Api.Services.Interfaces;

namespace GhostDataAudit.Api.Services;

public class AuditOrchestratorService : IAuditOrchestratorService
{
    private readonly ILogger<AuditOrchestratorService> _logger;

    public AuditOrchestratorService(ILogger<AuditOrchestratorService> logger)
    {
        _logger = logger;
    }

    public Task<CaseResult> AuditCaseAsync(DeletionRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Auditing case {CaseId}", request.CaseId);
        return Task.FromResult(new CaseResult
        {
            CaseId = request.CaseId,
            DeletionStatus = "INCOMPLETE",
            Steps = new List<AuditStep>()
        });
    }

    public Task<IReadOnlyList<CaseResult>> AuditAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Auditing all cases");
        IReadOnlyList<CaseResult> empty = Array.Empty<CaseResult>();
        return Task.FromResult(empty);
    }
}

