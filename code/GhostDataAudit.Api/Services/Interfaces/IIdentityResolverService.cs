using GhostDataAudit.Api.Models;

namespace GhostDataAudit.Api.Services.Interfaces;

public interface IIdentityResolverService
{
    Task<HashSet<string>> ResolveAllIdentifiersAsync(string userOrEmail, CancellationToken cancellationToken = default);
}

