using GhostDataAudit.Api.Models;
using GhostDataAudit.Api.Validators;

namespace GhostDataAudit.Api.Validators;

public interface IStepValidator
{
    bool ValidateStep(AuditStep step, out List<string> errors);
}

public class StepValidator : IStepValidator
{
    private static readonly HashSet<string> ValidPhases = new(StringComparer.Ordinal)
    {
        "FINDING", "REMEDIATION", "VERIFICATION"
    };

    private static readonly HashSet<string> ValidOperations = new(StringComparer.Ordinal)
    {
        "REPORT_REMAINING_RECORD", "REPORT_EXPOSED_PII", "REPORT_ACTIVE_DATA_FLOW", "REPORT_RESTORE_RISK",
        "DISABLE_DATA_FLOW", "BLOCK_RESTORE", "DELETE_RECORD", "REDACT_RECORD", "ANONYMIZE_RECORD",
        "VERIFY_RECORD_ABSENT", "VERIFY_RECORD_PRESENT", "VERIFY_RECORD_REDACTED", "VERIFY_RECORD_ANONYMIZED",
        "VERIFY_DATA_FLOW_DISABLED", "VERIFY_RESTORE_BLOCKED"
    };

    public bool ValidateStep(AuditStep step, out List<string> errors)
    {
        errors = new List<string>();

        if (step.StepNo <= 0)
            errors.Add($"StepNo must be positive, got {step.StepNo}");

        if (!ValidPhases.Contains(step.Phase))
            errors.Add($"Invalid phase: {step.Phase}");

        if (!ValidOperations.Contains(step.Operation))
            errors.Add($"Invalid operation: {step.Operation}");

        if (string.IsNullOrWhiteSpace(step.SystemId))
            errors.Add("SystemId cannot be empty");

        if (string.IsNullOrWhiteSpace(step.ResourceId))
            errors.Add("ResourceId cannot be empty");

        return errors.Count == 0;
    }
}

