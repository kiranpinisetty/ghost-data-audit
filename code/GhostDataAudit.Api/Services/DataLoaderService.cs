using GhostDataAudit.Api.Models;
using GhostDataAudit.Api.Services.Interfaces;

namespace GhostDataAudit.Api.Services;

public class DataLoaderService : IDataLoaderService
{
    private readonly ILogger<DataLoaderService> _logger;

    public DataLoaderService(ILogger<DataLoaderService> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<DeletionRequest>> LoadDeletionRequestsAsync(string filePath)
    {
        _logger.LogInformation("Loading deletion requests from {FilePath}", filePath);
        IReadOnlyList<DeletionRequest> empty = Array.Empty<DeletionRequest>();
        return Task.FromResult(empty);
    }

    public Task<IReadOnlyList<UserIdentity>> LoadUserIdentitiesAsync(string filePath)
    {
        _logger.LogInformation("Loading user identities from {FilePath}", filePath);
        IReadOnlyList<UserIdentity> empty = Array.Empty<UserIdentity>();
        return Task.FromResult(empty);
    }

    public Task<IReadOnlyList<UserAuditEntry>> LoadUserAuditLogAsync(string filePath)
    {
        _logger.LogInformation("Loading user audit log from {FilePath}", filePath);
        IReadOnlyList<UserAuditEntry> empty = Array.Empty<UserAuditEntry>();
        return Task.FromResult(empty);
    }

    public Task<IReadOnlyList<SystemActivity>> LoadSystemActivityAsync(string filePath)
    {
        _logger.LogInformation("Loading system activity from {FilePath}", filePath);
        IReadOnlyList<SystemActivity> empty = Array.Empty<SystemActivity>();
        return Task.FromResult(empty);
    }

    public Task<IReadOnlyList<DeletionRule>> LoadDeletionRulesAsync(string filePath)
    {
        _logger.LogInformation("Loading deletion rules from {FilePath}", filePath);
        IReadOnlyList<DeletionRule> empty = Array.Empty<DeletionRule>();
        return Task.FromResult(empty);
    }

    public Task<IReadOnlyList<EvidenceFile>> LoadEvidenceFilesAsync(string filePath)
    {
        _logger.LogInformation("Loading evidence files from {FilePath}", filePath);
        IReadOnlyList<EvidenceFile> empty = Array.Empty<EvidenceFile>();
        return Task.FromResult(empty);
    }
}

