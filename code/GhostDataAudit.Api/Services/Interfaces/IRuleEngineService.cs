using GhostDataAudit.Api.Models;

namespace GhostDataAudit.Api.Services.Interfaces;

/// <summary>
/// Applies deletion_rules.csv to evidence records and determines the required
/// action for each record. Decides whether deletion is COMPLETE or INCOMPLETE.
/// </summary>
public interface IRuleEngineService
{
    void Initialize(IReadOnlyList<DeletionRule> rules);

    /// <summary>
    /// Returns the required action for a data category: DELETE, REDACT, ANONYMIZE, or RETAIN.
    /// </summary>
    string GetRequiredAction(string dataCategory);
}

