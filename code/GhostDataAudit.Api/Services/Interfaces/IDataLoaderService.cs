using GhostDataAudit.Api.Models;

namespace GhostDataAudit.Api.Services.Interfaces;

public interface IDataLoaderService
{
    Task<IReadOnlyList<DeletionRequest>> LoadDeletionRequestsAsync(string filePath);
    Task<IReadOnlyList<UserIdentity>> LoadUserIdentitiesAsync(string filePath);
    Task<IReadOnlyList<UserAuditEntry>> LoadUserAuditLogAsync(string filePath);
    Task<IReadOnlyList<SystemActivity>> LoadSystemActivityAsync(string filePath);
    Task<IReadOnlyList<DeletionRule>> LoadDeletionRulesAsync(string filePath);
    Task<IReadOnlyList<EvidenceFile>> LoadEvidenceFilesAsync(string filePath);
}

