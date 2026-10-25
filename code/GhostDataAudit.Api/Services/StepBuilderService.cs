using GhostDataAudit.Api.Models;
using GhostDataAudit.Api.Services.Interfaces;

namespace GhostDataAudit.Api.Services;

/// <summary>
/// Builds ordered steps_json arrays following the challenge ordering rules:
/// - Phases: FINDING → REMEDIATION → VERIFICATION
/// - Within each phase: operations in table order, then system_id asc, then resource_id asc
/// - Remediation: disable data flows → block restores → delete/redact/anonymize
/// </summary>
public class StepBuilderService : IStepBuilderService
{
    // Operation ordering within FINDING phase
    private static readonly Dictionary<string, int> FindingOrder = new()
    {
        ["REPORT_REMAINING_RECORD"] = 0,
        ["REPORT_EXPOSED_PII"] = 1,
        ["REPORT_ACTIVE_DATA_FLOW"] = 2,
        ["REPORT_RESTORE_RISK"] = 3,
    };

    // Operation ordering within REMEDIATION phase
    private static readonly Dictionary<string, int> RemediationOrder = new()
    {
        ["DISABLE_DATA_FLOW"] = 0,
        ["BLOCK_RESTORE"] = 1,
        ["DELETE_RECORD"] = 2,
        ["REDACT_RECORD"] = 3,
        ["ANONYMIZE_RECORD"] = 4,
    };

    // Operation ordering within VERIFICATION phase
    private static readonly Dictionary<string, int> VerificationOrder = new()
    {
        ["VERIFY_RECORD_ABSENT"] = 0,
        ["VERIFY_RECORD_PRESENT"] = 1,
        ["VERIFY_RECORD_REDACTED"] = 2,
        ["VERIFY_RECORD_ANONYMIZED"] = 3,
        ["VERIFY_DATA_FLOW_DISABLED"] = 4,
        ["VERIFY_RESTORE_BLOCKED"] = 5,
    };

    public List<AuditStep> BuildIncompleteSteps(
        List<EvidenceRecord> findings,
        ResolvedIdentity identity,
        Func<string, string> requiredActionLookup)
    {
        var allSteps = new List<AuditStep>();

        // 1. FINDING steps — what's wrong
        var findingSteps = BuildFindingSteps(findings, requiredActionLookup);

        // 2. REMEDIATION steps — how to fix
        var remediationSteps = BuildRemediationSteps(findings, requiredActionLookup);

        // 3. VERIFICATION steps — how to confirm
        var verificationSteps = BuildIncompleteVerificationSteps(findings, identity, requiredActionLookup);

        allSteps.AddRange(SortSteps(findingSteps, FindingOrder));
        allSteps.AddRange(SortSteps(remediationSteps, RemediationOrder));
        allSteps.AddRange(SortSteps(verificationSteps, VerificationOrder));

        // Number consecutively
        for (int i = 0; i < allSteps.Count; i++)
            allSteps[i].StepNo = i + 1;

        return allSteps;
    }

    public List<AuditStep> BuildCompleteSteps(
        List<EvidenceRecord> verifiedRecords,
        ResolvedIdentity identity,
        Func<string, string> requiredActionLookup)
    {
        var steps = new List<AuditStep>();

        foreach (var rec in verifiedRecords)
        {
            var action = requiredActionLookup(rec.DataCategory);
            var (operation, state) = GetVerificationOpForAction(action, rec);

            steps.Add(new AuditStep
            {
                Phase = "VERIFICATION",
                SystemId = rec.SystemId,
                ResourceId = rec.ResourceId,
                Operation = operation,
                State = state,
                EvidenceId = rec.EvidenceFileId  // COMPLETE cases cite evidence
            });
        }

        var sorted = SortSteps(steps, VerificationOrder);
        for (int i = 0; i < sorted.Count; i++)
            sorted[i].StepNo = i + 1;

        return sorted;
    }

    private List<AuditStep> BuildFindingSteps(List<EvidenceRecord> findings, Func<string, string> requiredActionLookup)
    {
        var steps = new List<AuditStep>();

        foreach (var rec in findings)
        {
            var action = requiredActionLookup(rec.DataCategory);
            var (operation, state) = DetermineFindingOp(rec, action);

            steps.Add(new AuditStep
            {
                Phase = "FINDING",
                SystemId = rec.SystemId,
                ResourceId = rec.ResourceId,
                Operation = operation,
                State = state,
                EvidenceId = rec.EvidenceFileId  // Findings cite evidence
            });
        }

        return steps;
    }

    private List<AuditStep> BuildRemediationSteps(List<EvidenceRecord> findings, Func<string, string> requiredActionLookup)
    {
        var steps = new List<AuditStep>();

        foreach (var rec in findings)
        {
            var action = requiredActionLookup(rec.DataCategory);
            var (operation, targetState) = GetRemediationOp(rec, action);

            if (operation != null)
            {
                steps.Add(new AuditStep
                {
                    Phase = "REMEDIATION",
                    SystemId = rec.SystemId,
                    ResourceId = rec.ResourceId,
                    Operation = operation,
                    State = targetState,
                    EvidenceId = ""  // Remediation = future work
                });
            }
        }

        return steps;
    }

    private List<AuditStep> BuildIncompleteVerificationSteps(
        List<EvidenceRecord> findings,
        ResolvedIdentity identity,
        Func<string, string> requiredActionLookup)
    {
        var steps = new List<AuditStep>();
        var covered = new HashSet<string>();

        // Verification for each finding's remediated record
        foreach (var rec in findings)
        {
            var action = requiredActionLookup(rec.DataCategory);
            var (verifyOp, verifyState) = GetVerificationOpForAction(action, rec);
            var key = $"{rec.SystemId}|{rec.ResourceId}|{verifyOp}";
            if (covered.Add(key))
            {
                steps.Add(new AuditStep
                {
                    Phase = "VERIFICATION",
                    SystemId = rec.SystemId,
                    ResourceId = rec.ResourceId,
                    Operation = verifyOp,
                    State = verifyState,
                    EvidenceId = ""  // Future verification
                });
            }
        }

        return steps;
    }

    private static (string operation, string state) DetermineFindingOp(EvidenceRecord rec, string requiredAction)
    {
        // Active data flow
        if (rec.DataCategory == "DATA_FLOW" || (rec.IsEnabled == true))
            return ("REPORT_ACTIVE_DATA_FLOW", "ACTIVE");

        // Restore risk from backup
        if (rec.DataCategory == "BACKUP_ARCHIVE" && rec.RestoreAllowed == true)
            return ("REPORT_RESTORE_RISK", "ACTIVE");

        // Record that should have been anonymized or redacted but still has PII
        if (requiredAction == "ANONYMIZE" && rec.HasDirectIdentifiers)
            return ("REPORT_EXPOSED_PII", "PRESENT");

        if (requiredAction == "REDACT" && rec.HasDirectIdentifiers)
            return ("REPORT_EXPOSED_PII", "PRESENT");

        // Record that should be deleted but is still present
        return ("REPORT_REMAINING_RECORD", "PRESENT");
    }

    private static (string? operation, string targetState) GetRemediationOp(EvidenceRecord rec, string requiredAction)
    {
        if (rec.DataCategory == "DATA_FLOW" || rec.IsEnabled == true)
            return ("DISABLE_DATA_FLOW", "DISABLED");

        if (rec.DataCategory == "BACKUP_ARCHIVE" && rec.RestoreAllowed == true)
            return ("BLOCK_RESTORE", "BLOCKED");

        return requiredAction switch
        {
            "DELETE" => ("DELETE_RECORD", "ABSENT"),
            "REDACT" => ("REDACT_RECORD", "REDACTED"),
            "ANONYMIZE" => ("ANONYMIZE_RECORD", "ANONYMIZED"),
            "RETAIN" => (null, ""),  // No remediation for retained records
            _ => ("DELETE_RECORD", "ABSENT")
        };
    }

    private static (string operation, string state) GetVerificationOpForAction(string requiredAction, EvidenceRecord rec)
    {
        if (rec.DataCategory == "DATA_FLOW" || rec.IsEnabled != null)
            return ("VERIFY_DATA_FLOW_DISABLED", "DISABLED");

        if (rec.DataCategory == "BACKUP_ARCHIVE")
            return ("VERIFY_RESTORE_BLOCKED", "BLOCKED");

        if (rec.DataCategory == "SECURITY_AUDIT_LOG")
            return ("VERIFY_RECORD_PRESENT", "PRESENT");

        if (rec.DataCategory == "LEGAL_HOLD_RECORD")
            return ("VERIFY_RECORD_PRESENT", "PRESENT");

        return requiredAction switch
        {
            "DELETE" => ("VERIFY_RECORD_ABSENT", "ABSENT"),
            "REDACT" => ("VERIFY_RECORD_REDACTED", "REDACTED"),
            "ANONYMIZE" => ("VERIFY_RECORD_ANONYMIZED", "ANONYMIZED"),
            "RETAIN" => ("VERIFY_RECORD_PRESENT", "PRESENT"),
            _ => ("VERIFY_RECORD_ABSENT", "ABSENT")
        };
    }

    private static List<AuditStep> SortSteps(List<AuditStep> steps, Dictionary<string, int> operationOrder)
    {
        return steps
            .OrderBy(s => operationOrder.TryGetValue(s.Operation, out var o) ? o : 99)
            .ThenBy(s => s.SystemId, StringComparer.Ordinal)
            .ThenBy(s => s.ResourceId, StringComparer.Ordinal)
            .ToList();
    }
}

