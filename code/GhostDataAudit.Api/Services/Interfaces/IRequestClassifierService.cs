using GhostDataAudit.Api.Models.Enums;

namespace GhostDataAudit.Api.Services.Interfaces;

public interface IRequestClassifierService
{
    Task<DeletionStatus> ClassifyRequestAsync(string requestText, CancellationToken cancellationToken = default);
}

