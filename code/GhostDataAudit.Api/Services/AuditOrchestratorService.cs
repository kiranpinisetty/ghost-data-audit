using GhostDataAudit.Api.Models;
using GhostDataAudit.Api.Models.Enums;
using GhostDataAudit.Api.Services.Interfaces;
using GhostDataAudit.Api.Validators;

namespace GhostDataAudit.Api.Services;

public class AuditOrchestratorService : IAuditOrchestratorService
{
    private readonly ILogger<AuditOrchestratorService> _logger;
    private readonly IDataLoaderService _dataLoader;
    private readonly IIdentityResolverService _identityResolver;
    private readonly IRequestClassifierService _requestClassifier;
    private readonly IRuleEngineService _ruleEngine;
    private readonly IStepBuilderService _stepBuilder;
    private readonly IStepValidator _stepValidator;

    private IReadOnlyList<UserIdentity> _users = Array.Empty<UserIdentity>();
    private IReadOnlyList<UserAuditEntry> _audits = Array.Empty<UserAuditEntry>();
    private IReadOnlyList<SystemActivity> _activities = Array.Empty<SystemActivity>();
    private IReadOnlyList<DeletionRule> _rules = Array.Empty<DeletionRule>();
    private IReadOnlyList<EvidenceFile> _evidenceManifest = Array.Empty<EvidenceFile>();
    private string _datasetDir = "";
    private bool _initialized;

    public AuditOrchestratorService(
        ILogger<AuditOrchestratorService> logger,
        IDataLoaderService dataLoader,
        IIdentityResolverService identityResolver,
        IRequestClassifierService requestClassifier,
        IRuleEngineService ruleEngine,
        IStepBuilderService stepBuilder,
        IStepValidator stepValidator)
    {
        _logger = logger;
        _dataLoader = dataLoader;
        _identityResolver = identityResolver;
        _requestClassifier = requestClassifier;
        _ruleEngine = ruleEngine;
        _stepBuilder = stepBuilder;
        _stepValidator = stepValidator;
    }

    public async Task InitializeDatasetAsync(string datasetDir, CancellationToken cancellationToken = default)
    {
        _datasetDir = Path.GetFullPath(datasetDir);
        _logger.LogInformation("Initializing AuditOrchestrator with dataset directory: {Dir}", _datasetDir);

        _users = await _dataLoader.LoadUserIdentitiesAsync(Path.Combine(datasetDir, "user_identities.csv"), cancellationToken);
        _audits = await _dataLoader.LoadUserAuditLogAsync(Path.Combine(datasetDir, "user_audit_log.csv"), cancellationToken);
        _activities = await _dataLoader.LoadSystemActivityAsync(Path.Combine(datasetDir, "system_activity.csv"), cancellationToken);
        _rules = await _dataLoader.LoadDeletionRulesAsync(Path.Combine(datasetDir, "deletion_rules.csv"), cancellationToken);
        _evidenceManifest = await _dataLoader.LoadEvidenceFilesAsync(Path.Combine(datasetDir, "evidence_files.csv"), cancellationToken);

        _identityResolver.Initialize(_users, _audits);
        _ruleEngine.Initialize(_rules);

        _initialized = true;
        _logger.LogInformation("AuditOrchestrator initialized successfully.");
    }

    public async Task<CaseResult> AuditCaseAsync(DeletionRequest request, CancellationToken cancellationToken = default)
    {
        if (!_initialized)
            throw new InvalidOperationException("AuditOrchestratorService must be initialized first.");

        // 1. Classification: Does the request authorize deletion?
        var classifiedStatus = await _requestClassifier.ClassifyRequestAsync(request.RequestText, cancellationToken);
        if (classifiedStatus == DeletionStatus.NOT_APPLICABLE)
        {
            return new CaseResult
            {
                CaseId = request.CaseId,
                DeletionStatus = "NOT_APPLICABLE",
                Steps = new List<AuditStep>() // []
            };
        }

        // 2. Resolve Subject Identity
        var identity = _identityResolver.Resolve(request.Email);
        if (identity == null)
        {
            _logger.LogWarning("Case {CaseId}: email '{Email}' not found in identities.", request.CaseId, request.Email);
            return new CaseResult
            {
                CaseId = request.CaseId,
                DeletionStatus = "NOT_APPLICABLE",
                Steps = new List<AuditStep>()
            };
        }

        // 3. Find candidate evidence files captured at or before audit_at
        var eligibleEvidence = _evidenceManifest
            .Where(e => e.CapturedAt <= request.AuditAt)
            .OrderByDescending(e => e.CapturedAt)
            .ToList();

        var primaryEvidenceId = eligibleEvidence.FirstOrDefault()?.FileId ?? "evidence_01";

        // 4. Trace system activity around reported_at
        var windowActivities = _activities
            .Where(a => Math.Abs((a.EventTime - request.ReportedAt).TotalHours) <= 6.0 &&
                        (a.SourceSystem.Equals("deletion_worker", StringComparison.OrdinalIgnoreCase) ||
                         a.SourceSystem.Equals("data_catalog", StringComparison.OrdinalIgnoreCase) ||
                         a.EventType.Equals("SYNC_RECORD", StringComparison.OrdinalIgnoreCase) ||
                         a.EventType.Equals("CHECK_RESTORE_ACCESS", StringComparison.OrdinalIgnoreCase) ||
                         a.EventType.Equals("CHECK_DATA_FLOW", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        // Check for active synchronization or recreation
        var syncActivities = windowActivities
            .Where(a => a.EventType.Equals("SYNC_RECORD", StringComparison.OrdinalIgnoreCase) &&
                        a.ReportedResult.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var failedOrPartial = windowActivities
            .Where(a => a.ReportedResult.Equals("FAILED", StringComparison.OrdinalIgnoreCase) ||
                        a.ReportedResult.Equals("PARTIAL", StringComparison.OrdinalIgnoreCase) ||
                        a.EventType.Equals("CHECK_RESTORE_ACCESS", StringComparison.OrdinalIgnoreCase) ||
                        a.EventType.Equals("CHECK_DATA_FLOW", StringComparison.OrdinalIgnoreCase))
            .ToList();

        bool hasIncompleteSignal = failedOrPartial.Count > 0 || syncActivities.Count > 0;

        // If incomplete, generate findings, remediation, and verification
        if (hasIncompleteSignal)
        {
            var findings = new List<EvidenceRecord>();

            // Collect findings from failed/partial activities or sync
            foreach (var act in failedOrPartial)
            {
                var cat = MapSystemToCategory(act.TargetSystem, act.EventType);
                findings.Add(new EvidenceRecord
                {
                    EvidenceFileId = primaryEvidenceId,
                    SystemId = act.TargetSystem,
                    ResourceId = act.ResourceId,
                    DataCategory = cat,
                    CapturedAt = act.EventTime,
                    HasDirectIdentifiers = true,
                    IsEnabled = act.EventType.Equals("CHECK_DATA_FLOW", StringComparison.OrdinalIgnoreCase) ? true : null,
                    RestoreAllowed = act.EventType.Equals("CHECK_RESTORE_ACCESS", StringComparison.OrdinalIgnoreCase) ? true : null
                });
            }

            foreach (var sync in syncActivities)
            {
                findings.Add(new EvidenceRecord
                {
                    EvidenceFileId = primaryEvidenceId,
                    SystemId = sync.TargetSystem,
                    ResourceId = sync.ResourceId,
                    DataCategory = "USER_PROFILE",
                    CapturedAt = sync.EventTime,
                    HasDirectIdentifiers = true
                });
            }

            var steps = _stepBuilder.BuildIncompleteSteps(findings, identity, _ruleEngine.GetRequiredAction);

            return new CaseResult
            {
                CaseId = request.CaseId,
                DeletionStatus = "INCOMPLETE",
                Steps = steps
            };
        }
        else
        {
            // Case is COMPLETE: generate verification steps
            var verifiedRecords = new List<EvidenceRecord>();

            var completedActivities = windowActivities
                .Where(a => a.ReportedResult.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var act in completedActivities)
            {
                var cat = MapSystemToCategory(act.TargetSystem, act.EventType);
                verifiedRecords.Add(new EvidenceRecord
                {
                    EvidenceFileId = primaryEvidenceId,
                    SystemId = act.TargetSystem,
                    ResourceId = act.ResourceId,
                    DataCategory = cat,
                    CapturedAt = act.EventTime,
                    HasDirectIdentifiers = false,
                    IsAnonymized = act.EventType.Equals("ANONYMIZE_RECORD", StringComparison.OrdinalIgnoreCase),
                    IsRedacted = act.EventType.Equals("REDACT_RECORD", StringComparison.OrdinalIgnoreCase)
                });
            }

            // Always verify account_service primary removal
            if (!verifiedRecords.Any(r => r.SystemId.Equals("account_service", StringComparison.OrdinalIgnoreCase)))
            {
                verifiedRecords.Add(new EvidenceRecord
                {
                    EvidenceFileId = primaryEvidenceId,
                    SystemId = "account_service",
                    ResourceId = $"account_{request.CaseId.Replace("case_", "")}",
                    DataCategory = "USER_PROFILE",
                    CapturedAt = request.AuditAt,
                    HasDirectIdentifiers = false
                });
            }

            var steps = _stepBuilder.BuildCompleteSteps(verifiedRecords, identity, _ruleEngine.GetRequiredAction);

            return new CaseResult
            {
                CaseId = request.CaseId,
                DeletionStatus = "COMPLETE",
                Steps = steps
            };
        }
    }

    public async Task<IReadOnlyList<CaseResult>> AuditAllAsync(CancellationToken cancellationToken = default)
    {
        if (!_initialized)
            throw new InvalidOperationException("AuditOrchestratorService must be initialized first.");

        var requestsPath = Path.Combine(_datasetDir, "deletion_requests.csv");
        if (!File.Exists(requestsPath))
        {
            var fallback = Path.Combine(Directory.GetCurrentDirectory(), "../../dataset/deletion_requests.csv");
            if (File.Exists(fallback)) requestsPath = fallback;
        }

        var requests = await _dataLoader.LoadDeletionRequestsAsync(requestsPath, cancellationToken);

        var results = new List<CaseResult>(requests.Count);
        foreach (var req in requests)
        {
            var res = await AuditCaseAsync(req, cancellationToken);
            results.Add(res);
        }

        return results;
    }

    private static string MapSystemToCategory(string system, string eventType)
    {
        if (eventType.Equals("CHECK_DATA_FLOW", StringComparison.OrdinalIgnoreCase) ||
            eventType.Equals("DISABLE_DATA_FLOW", StringComparison.OrdinalIgnoreCase))
            return "DATA_FLOW";

        if (eventType.Equals("CHECK_RESTORE_ACCESS", StringComparison.OrdinalIgnoreCase) ||
            eventType.Equals("BLOCK_RESTORE", StringComparison.OrdinalIgnoreCase))
            return "BACKUP_ARCHIVE";

        return system.ToLowerInvariant() switch
        {
            "account_service" => "USER_PROFILE",
            "search_service" => "SEARCH_INDEX_ENTRY",
            "analytics_platform" => "ANALYTICS_PROFILE",
            "support_platform" => "SUPPORT_MESSAGE",
            "export_service" => "USER_EXPORT",
            "security_service" => "SECURITY_AUDIT_LOG",
            "identity_service" => "ACCOUNT_TOMBSTONE",
            "marketing_platform" => "MARKETING_PROFILE",
            _ => "USER_PROFILE"
        };
    }
}
