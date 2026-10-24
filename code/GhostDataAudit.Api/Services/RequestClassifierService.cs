using GhostDataAudit.Api.Models.Enums;
using GhostDataAudit.Api.Services.Interfaces;

namespace GhostDataAudit.Api.Services;

public class RequestClassifierService : IRequestClassifierService
{
    private readonly ILogger<RequestClassifierService> _logger;

    public RequestClassifierService(ILogger<RequestClassifierService> logger)
    {
        _logger = logger;
    }

    public Task<DeletionStatus> ClassifyRequestAsync(string requestText, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Classifying request: {Snippet}...", requestText.Length > 30 ? requestText[..30] : requestText);
        // Skeleton logic for Phase 1
        return Task.FromResult(DeletionStatus.INCOMPLETE);
    }
}

