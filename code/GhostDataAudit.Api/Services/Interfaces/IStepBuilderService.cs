using GhostDataAudit.Api.Models;

namespace GhostDataAudit.Api.Services.Interfaces;

/// <summary>
/// Builds the ordered steps_json array from evidence records and rule decisions.
/// </summary>
public interface IStepBuilderService
{
    /// <summary>
    /// Build the steps for an INCOMPLETE case: findings, remediation, verification.
    /// </summary>
    List<AuditStep> BuildIncompleteSteps(List<EvidenceRecord> findings, ResolvedIdentity identity,
        Func<string, string> requiredActionLookup);

    /// <summary>
    /// Build the verification-only steps for a COMPLETE case.
    /// </summary>
    List<AuditStep> BuildCompleteSteps(List<EvidenceRecord> verifiedRecords, ResolvedIdentity identity,
        Func<string, string> requiredActionLookup);
}
