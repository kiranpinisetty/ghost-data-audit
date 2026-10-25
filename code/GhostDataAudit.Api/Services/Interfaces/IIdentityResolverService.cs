using GhostDataAudit.Api.Models;

namespace GhostDataAudit.Api.Services.Interfaces;

/// <summary>
/// Resolves a deletion-request email to the full set of current and historical
/// identifiers for the associated user.
/// </summary>
public interface IIdentityResolverService
{
    /// <summary>
    /// Initialize the resolver with loaded dataset tables.
    /// Must be called once before Resolve calls.
    /// </summary>
    void Initialize(IReadOnlyList<UserIdentity> users, IReadOnlyList<UserAuditEntry> auditLog);

    /// <summary>
    /// Resolve an email to the user's full identity set.
    /// Returns null if the email cannot be matched to any known user.
    /// </summary>
    ResolvedIdentity? Resolve(string email);
}
