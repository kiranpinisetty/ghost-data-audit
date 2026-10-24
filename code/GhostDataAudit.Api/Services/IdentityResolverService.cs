using GhostDataAudit.Api.Services.Interfaces;

namespace GhostDataAudit.Api.Services;

public class IdentityResolverService : IIdentityResolverService
{
    private readonly ILogger<IdentityResolverService> _logger;

    public IdentityResolverService(ILogger<IdentityResolverService> logger)
    {
        _logger = logger;
    }

    public Task<HashSet<string>> ResolveAllIdentifiersAsync(string userOrEmail, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resolving identifiers for {Subject}", userOrEmail);
        return Task.FromResult(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { userOrEmail });
    }
}

