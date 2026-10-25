using System.Globalization;
using GhostDataAudit.Api.Data;
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

    public async Task<IReadOnlyList<DeletionRequest>> LoadDeletionRequestsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var rows = await CsvReaderUtility.ParseFileAsync(filePath, cancellationToken);
        if (rows.Count == 0)
            return Array.Empty<DeletionRequest>();

        var header = rows[0];
        ValidateHeader(filePath, header, new[] { "case_id", "email", "requested_at", "reported_status", "reported_at", "audit_at", "request_text" });

        var list = new List<DeletionRequest>(rows.Count - 1);
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 1; i < rows.Count; i++)
        {
            var r = rows[i];
            if (r.Count < 7)
                throw new InvalidDataException($"Line {i + 1} in {filePath} is malformed: expected 7 columns, got {r.Count}");

            var caseId = r[0].Trim();
            if (string.IsNullOrEmpty(caseId))
                throw new InvalidDataException($"Line {i + 1} in {filePath} has empty case_id");

            if (!seenIds.Add(caseId))
                throw new InvalidDataException($"Duplicate case_id '{caseId}' found on line {i + 1} in {filePath}");

            var email = r[1].Trim();
            if (string.IsNullOrEmpty(email))
                throw new InvalidDataException($"Line {i + 1} in {filePath} has empty email");

            if (!DateTime.TryParse(r[2].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var requestedAt))
                throw new InvalidDataException($"Line {i + 1} in {filePath} has invalid requested_at: '{r[2]}'");

            var reportedStatus = r[3].Trim();
            if (!DateTime.TryParse(r[4].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var reportedAt))
                throw new InvalidDataException($"Line {i + 1} in {filePath} has invalid reported_at: '{r[4]}'");

            if (!DateTime.TryParse(r[5].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var auditAt))
                throw new InvalidDataException($"Line {i + 1} in {filePath} has invalid audit_at: '{r[5]}'");

            var requestText = r[6];

            list.Add(new DeletionRequest
            {
                CaseId = caseId,
                Email = email,
                RequestedAt = requestedAt,
                ReportedStatus = reportedStatus,
                ReportedAt = reportedAt,
                AuditAt = auditAt,
                RequestText = requestText
            });
        }

        return list;
    }

    public async Task<IReadOnlyList<UserIdentity>> LoadUserIdentitiesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var rows = await CsvReaderUtility.ParseFileAsync(filePath, cancellationToken);
        if (rows.Count == 0)
            return Array.Empty<UserIdentity>();

        var header = rows[0];
        ValidateHeader(filePath, header, new[] { "user_id", "name", "email", "phone", "email_hash", "updated_at" });

        var list = new List<UserIdentity>(rows.Count - 1);
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 1; i < rows.Count; i++)
        {
            var r = rows[i];
            if (r.Count < 6)
                throw new InvalidDataException($"Line {i + 1} in {filePath} is malformed: expected 6 columns, got {r.Count}");

            var userId = r[0].Trim();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidDataException($"Line {i + 1} in {filePath} has empty user_id");

            if (!seenIds.Add(userId))
                throw new InvalidDataException($"Duplicate user_id '{userId}' on line {i + 1} in {filePath}");

            if (!DateTime.TryParse(r[5].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var updatedAt))
                throw new InvalidDataException($"Line {i + 1} in {filePath} has invalid updated_at: '{r[5]}'");

            list.Add(new UserIdentity
            {
                UserId = userId,
                Name = r[1].Trim(),
                Email = r[2].Trim(),
                Phone = r[3].Trim(),
                EmailHash = r[4].Trim(),
                UpdatedAt = updatedAt
            });
        }

        return list;
    }

    public async Task<IReadOnlyList<UserAuditEntry>> LoadUserAuditLogAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var rows = await CsvReaderUtility.ParseFileAsync(filePath, cancellationToken);
        if (rows.Count == 0)
            return Array.Empty<UserAuditEntry>();

        var header = rows[0];
        ValidateHeader(filePath, header, new[] { "audit_id", "user_id", "event_time", "field_name", "old_value", "new_value" });

        var list = new List<UserAuditEntry>(rows.Count - 1);
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 1; i < rows.Count; i++)
        {
            var r = rows[i];
            if (r.Count < 6)
                throw new InvalidDataException($"Line {i + 1} in {filePath} is malformed: expected 6 columns, got {r.Count}");

            var auditId = r[0].Trim();
            if (string.IsNullOrEmpty(auditId))
                throw new InvalidDataException($"Line {i + 1} in {filePath} has empty audit_id");

            if (!seenIds.Add(auditId))
                throw new InvalidDataException($"Duplicate audit_id '{auditId}' on line {i + 1} in {filePath}");

            if (!DateTime.TryParse(r[2].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var eventTime))
                throw new InvalidDataException($"Line {i + 1} in {filePath} has invalid event_time: '{r[2]}'");

            list.Add(new UserAuditEntry
            {
                AuditId = auditId,
                UserId = r[1].Trim(),
                EventTime = eventTime,
                FieldName = r[3].Trim(),
                OldValue = r[4].Trim(),
                NewValue = r[5].Trim()
            });
        }

        return list;
    }

    public async Task<IReadOnlyList<SystemActivity>> LoadSystemActivityAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var rows = await CsvReaderUtility.ParseFileAsync(filePath, cancellationToken);
        if (rows.Count == 0)
            return Array.Empty<SystemActivity>();

        var header = rows[0];
        ValidateHeader(filePath, header, new[] { "activity_id", "event_time", "event_type", "source_system", "target_system", "resource_id", "reported_result", "details" });

        var list = new List<SystemActivity>(rows.Count - 1);
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 1; i < rows.Count; i++)
        {
            var r = rows[i];
            if (r.Count < 8)
                throw new InvalidDataException($"Line {i + 1} in {filePath} is malformed: expected 8 columns, got {r.Count}");

            var actId = r[0].Trim();
            if (string.IsNullOrEmpty(actId))
                throw new InvalidDataException($"Line {i + 1} in {filePath} has empty activity_id");

            if (!seenIds.Add(actId))
                throw new InvalidDataException($"Duplicate activity_id '{actId}' on line {i + 1} in {filePath}");

            if (!DateTime.TryParse(r[1].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var eventTime))
                throw new InvalidDataException($"Line {i + 1} in {filePath} has invalid event_time: '{r[1]}'");

            list.Add(new SystemActivity
            {
                ActivityId = actId,
                EventTime = eventTime,
                EventType = r[2].Trim(),
                SourceSystem = r[3].Trim(),
                TargetSystem = r[4].Trim(),
                ResourceId = r[5].Trim(),
                ReportedResult = r[6].Trim(),
                Details = r[7]
            });
        }

        return list;
    }

    public async Task<IReadOnlyList<DeletionRule>> LoadDeletionRulesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var rows = await CsvReaderUtility.ParseFileAsync(filePath, cancellationToken);
        if (rows.Count == 0)
            return Array.Empty<DeletionRule>();

        var header = rows[0];
        ValidateHeader(filePath, header, new[] { "rule_id", "data_category", "condition", "required_action" });

        var list = new List<DeletionRule>(rows.Count - 1);
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 1; i < rows.Count; i++)
        {
            var r = rows[i];
            if (r.Count < 4)
                throw new InvalidDataException($"Line {i + 1} in {filePath} is malformed: expected 4 columns, got {r.Count}");

            var ruleId = r[0].Trim();
            if (string.IsNullOrEmpty(ruleId))
                throw new InvalidDataException($"Line {i + 1} in {filePath} has empty rule_id");

            if (!seenIds.Add(ruleId))
                throw new InvalidDataException($"Duplicate rule_id '{ruleId}' on line {i + 1} in {filePath}");

            list.Add(new DeletionRule
            {
                RuleId = ruleId,
                DataCategory = r[1].Trim(),
                Condition = r[2].Trim(),
                RequiredAction = r[3].Trim()
            });
        }

        return list;
    }

    public async Task<IReadOnlyList<EvidenceFile>> LoadEvidenceFilesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var rows = await CsvReaderUtility.ParseFileAsync(filePath, cancellationToken);
        if (rows.Count == 0)
            return Array.Empty<EvidenceFile>();

        var header = rows[0];
        ValidateHeader(filePath, header, new[] { "file_id", "file_path", "origin", "captured_at" });

        var list = new List<EvidenceFile>(rows.Count - 1);
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 1; i < rows.Count; i++)
        {
            var r = rows[i];
            if (r.Count < 4)
                throw new InvalidDataException($"Line {i + 1} in {filePath} is malformed: expected 4 columns, got {r.Count}");

            var fileId = r[0].Trim();
            if (string.IsNullOrEmpty(fileId))
                throw new InvalidDataException($"Line {i + 1} in {filePath} has empty file_id");

            if (!seenIds.Add(fileId))
                throw new InvalidDataException($"Duplicate file_id '{fileId}' on line {i + 1} in {filePath}");

            if (!DateTime.TryParse(r[3].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var capturedAt))
                throw new InvalidDataException($"Line {i + 1} in {filePath} has invalid captured_at: '{r[3]}'");

            list.Add(new EvidenceFile
            {
                FileId = fileId,
                FilePath = r[1].Trim(),
                Origin = r[2].Trim(),
                CapturedAt = capturedAt
            });
        }

        return list;
    }

    public string ResolveSafeEvidencePath(string baseDatasetDirectory, string relativeEvidencePath)
    {
        if (string.IsNullOrWhiteSpace(baseDatasetDirectory))
            throw new ArgumentException("Base dataset directory cannot be empty.", nameof(baseDatasetDirectory));

        if (string.IsNullOrWhiteSpace(relativeEvidencePath))
            throw new ArgumentException("Evidence path cannot be empty.", nameof(relativeEvidencePath));

        // Normalize separators
        var normalizedRelative = relativeEvidencePath.Replace('\\', '/').TrimStart('/');

        // Resolve absolute canonical path
        var fullBasePath = Path.GetFullPath(baseDatasetDirectory);
        var combinedPath = Path.GetFullPath(Path.Combine(fullBasePath, normalizedRelative));

        // Prevent directory traversal attacks
        if (!combinedPath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Directory traversal detected: '{relativeEvidencePath}' resolves outside base dataset folder '{baseDatasetDirectory}'.");
        }

        return combinedPath;
    }

    public async Task<DatasetDiagnosticReport> ValidateAndDiagnoseDatasetAsync(string datasetDirectory, CancellationToken cancellationToken = default)
    {
        var report = new DatasetDiagnosticReport();
        var fullDatasetDir = Path.GetFullPath(datasetDirectory);

        if (!Directory.Exists(fullDatasetDir))
        {
            report.ValidationErrors.Add($"Dataset directory does not exist: {fullDatasetDir}");
            return report;
        }

        // 1. Load files
        IReadOnlyList<DeletionRequest> requests = Array.Empty<DeletionRequest>();
        IReadOnlyList<DeletionRequest> sampleRequests = Array.Empty<DeletionRequest>();
        IReadOnlyList<UserIdentity> users = Array.Empty<UserIdentity>();
        IReadOnlyList<UserAuditEntry> audits = Array.Empty<UserAuditEntry>();
        IReadOnlyList<SystemActivity> activities = Array.Empty<SystemActivity>();
        IReadOnlyList<DeletionRule> rules = Array.Empty<DeletionRule>();
        IReadOnlyList<EvidenceFile> evidence = Array.Empty<EvidenceFile>();

        try
        {
            var p = Path.Combine(fullDatasetDir, "deletion_requests.csv");
            if (File.Exists(p))
            {
                requests = await LoadDeletionRequestsAsync(p, cancellationToken);
                report.DeletionRequestsCount = requests.Count;
            }
            else report.ValidationErrors.Add("Missing deletion_requests.csv");
        }
        catch (Exception ex) { report.MalformedRecords.Add($"deletion_requests.csv error: {ex.Message}"); }

        try
        {
            var p = Path.Combine(fullDatasetDir, "sample_deletion_requests.csv");
            if (File.Exists(p))
            {
                sampleRequests = await LoadDeletionRequestsAsync(p, cancellationToken);
                report.SampleDeletionRequestsCount = sampleRequests.Count;
            }
            else report.ValidationErrors.Add("Missing sample_deletion_requests.csv");
        }
        catch (Exception ex) { report.MalformedRecords.Add($"sample_deletion_requests.csv error: {ex.Message}"); }

        try
        {
            var p = Path.Combine(fullDatasetDir, "user_identities.csv");
            if (File.Exists(p))
            {
                users = await LoadUserIdentitiesAsync(p, cancellationToken);
                report.UserIdentitiesCount = users.Count;
            }
            else report.ValidationErrors.Add("Missing user_identities.csv");
        }
        catch (Exception ex) { report.MalformedRecords.Add($"user_identities.csv error: {ex.Message}"); }

        try
        {
            var p = Path.Combine(fullDatasetDir, "user_audit_log.csv");
            if (File.Exists(p))
            {
                audits = await LoadUserAuditLogAsync(p, cancellationToken);
                report.UserAuditLogCount = audits.Count;
            }
            else report.ValidationErrors.Add("Missing user_audit_log.csv");
        }
        catch (Exception ex) { report.MalformedRecords.Add($"user_audit_log.csv error: {ex.Message}"); }

        try
        {
            var p = Path.Combine(fullDatasetDir, "system_activity.csv");
            if (File.Exists(p))
            {
                activities = await LoadSystemActivityAsync(p, cancellationToken);
                report.SystemActivityCount = activities.Count;
            }
            else report.ValidationErrors.Add("Missing system_activity.csv");
        }
        catch (Exception ex) { report.MalformedRecords.Add($"system_activity.csv error: {ex.Message}"); }

        try
        {
            var p = Path.Combine(fullDatasetDir, "deletion_rules.csv");
            if (File.Exists(p))
            {
                rules = await LoadDeletionRulesAsync(p, cancellationToken);
                report.DeletionRulesCount = rules.Count;
            }
            else report.ValidationErrors.Add("Missing deletion_rules.csv");
        }
        catch (Exception ex) { report.MalformedRecords.Add($"deletion_rules.csv error: {ex.Message}"); }

        try
        {
            var p = Path.Combine(fullDatasetDir, "evidence_files.csv");
            if (File.Exists(p))
            {
                evidence = await LoadEvidenceFilesAsync(p, cancellationToken);
                report.EvidenceFilesCount = evidence.Count;
            }
            else report.ValidationErrors.Add("Missing evidence_files.csv");
        }
        catch (Exception ex) { report.MalformedRecords.Add($"evidence_files.csv error: {ex.Message}"); }

        // 2. Cross-reference validations
        var userIds = new HashSet<string>(users.Select(u => u.UserId), StringComparer.OrdinalIgnoreCase);
        var allKnownEmails = new HashSet<string>(users.Select(u => u.Email), StringComparer.OrdinalIgnoreCase);

        foreach (var audit in audits)
        {
            if (audit.FieldName.Equals("email", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(audit.OldValue)) allKnownEmails.Add(audit.OldValue);
                if (!string.IsNullOrEmpty(audit.NewValue)) allKnownEmails.Add(audit.NewValue);
            }

            if (!userIds.Contains(audit.UserId))
            {
                report.MissingReferences.Add($"Audit entry {audit.AuditId} references unknown user_id '{audit.UserId}'");
            }
        }

        // Validate deletion requests map to known user identities/audits
        foreach (var req in requests)
        {
            if (!allKnownEmails.Contains(req.Email))
            {
                report.MissingReferences.Add($"Deletion request {req.CaseId} email '{req.Email}' cannot be resolved in user identities or audit logs.");
            }
        }

        // 3. Evidence file accessibility & path traversal verification
        foreach (var ev in evidence)
        {
            try
            {
                var resolvedPath = ResolveSafeEvidencePath(fullDatasetDir, ev.FilePath);
                if (!File.Exists(resolvedPath))
                {
                    report.InaccessibleEvidenceFiles.Add($"Evidence file '{ev.FileId}' not found at resolved path: {resolvedPath}");
                }
            }
            catch (Exception ex)
            {
                report.InaccessibleEvidenceFiles.Add($"Evidence file '{ev.FileId}' path resolution error: {ex.Message}");
            }
        }

        return report;
    }

    private static void ValidateHeader(string filePath, IReadOnlyList<string> actualHeader, IReadOnlyList<string> expectedColumns)
    {
        if (actualHeader.Count < expectedColumns.Count)
        {
            throw new InvalidDataException($"Header row in {filePath} is missing columns. Expected at least {expectedColumns.Count}, got {actualHeader.Count}.");
        }

        for (int i = 0; i < expectedColumns.Count; i++)
        {
            if (!actualHeader[i].Trim().Equals(expectedColumns[i], StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Header column {i + 1} in {filePath} is '{actualHeader[i]}', expected '{expectedColumns[i]}'.");
            }
        }
    }
}
