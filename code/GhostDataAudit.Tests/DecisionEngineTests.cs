using GhostDataAudit.Api.Models;
using GhostDataAudit.Api.Models.Enums;
using GhostDataAudit.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GhostDataAudit.Tests;

public class DecisionEngineTests
{
    private readonly RequestClassifierService _classifier;
    private readonly RuleEngineService _ruleEngine;
    private readonly StepBuilderService _stepBuilder;

    public DecisionEngineTests()
    {
        _classifier = new RequestClassifierService(NullLogger<RequestClassifierService>.Instance);
        _ruleEngine = new RuleEngineService(NullLogger<RuleEngineService>.Instance);
        _stepBuilder = new StepBuilderService();

        var sampleRules = new List<DeletionRule>
        {
            new() { RuleId = "rule_01", DataCategory = "USER_PROFILE", Condition = "ALL", RequiredAction = "DELETE" },
            new() { RuleId = "rule_02", DataCategory = "ACCOUNT_TOMBSTONE", Condition = "ALL", RequiredAction = "REDACT" },
            new() { RuleId = "rule_03", DataCategory = "SECURITY_AUDIT_LOG", Condition = "ALL", RequiredAction = "RETAIN" },
            new() { RuleId = "rule_04", DataCategory = "ANALYTICS_PROFILE", Condition = "ALL", RequiredAction = "ANONYMIZE" },
            new() { RuleId = "rule_05", DataCategory = "DATA_FLOW", Condition = "ALL", RequiredAction = "DISABLE" },
            new() { RuleId = "rule_06", DataCategory = "BACKUP_ARCHIVE", Condition = "ALL", RequiredAction = "BLOCK" }
        };
        _ruleEngine.Initialize(sampleRules);
    }

    [Theory]
    [InlineData("I would like to permanently delete my account and all associated personal data.")]
    [InlineData("Bitte löschen Sie alle meine personenbezogenen Daten gemäß DSGVO.")]
    [InlineData("Por favor elimine todos mis datos personales de sus sistemas.")]
    [InlineData("Veuillez supprimer définitivement mon compte et mes données.")]
    public async Task RequestClassifier_DeletionRequests_DoesNotReturnNotApplicable(string requestText)
    {
        var result = await _classifier.ClassifyRequestAsync(requestText);
        Assert.NotEqual(DeletionStatus.NOT_APPLICABLE, result);
    }

    [Theory]
    [InlineData("Please unsubscribe me from marketing emails and newsletter promotions.")]
    [InlineData("I want to export a copy of all my orders and invoices.")]
    [InlineData("I cannot log in to my account, please reset my password.")]
    [InlineData("Could you please send me a receipt for my last invoice?")]
    public async Task RequestClassifier_NonDeletionRequests_ReturnsNotApplicable(string requestText)
    {
        var result = await _classifier.ClassifyRequestAsync(requestText);
        Assert.Equal(DeletionStatus.NOT_APPLICABLE, result);
    }

    [Fact]
    public void RuleEngine_KnownCategories_ReturnsExpectedActions()
    {
        Assert.Equal("DELETE", _ruleEngine.GetRequiredAction("USER_PROFILE"));
        Assert.Equal("REDACT", _ruleEngine.GetRequiredAction("ACCOUNT_TOMBSTONE"));
        Assert.Equal("RETAIN", _ruleEngine.GetRequiredAction("SECURITY_AUDIT_LOG"));
        Assert.Equal("ANONYMIZE", _ruleEngine.GetRequiredAction("ANALYTICS_PROFILE"));
    }

    [Fact]
    public void StepBuilder_IncompleteCase_FollowsStrictOrderingAndEmptyEvidenceForRemediation()
    {
        var identity = new ResolvedIdentity
        {
            UserId = "user_100",
            CurrentName = "Test User",
            AllEmails = { "user@test.com" }
        };

        var findings = new List<EvidenceRecord>
        {
            new()
            {
                EvidenceFileId = "evidence_01",
                SystemId = "sync_service",
                ResourceId = "flow_01",
                DataCategory = "DATA_FLOW",
                IsEnabled = true
            },
            new()
            {
                EvidenceFileId = "evidence_02",
                SystemId = "backup_service",
                ResourceId = "backup_01",
                DataCategory = "BACKUP_ARCHIVE",
                RestoreAllowed = true
            },
            new()
            {
                EvidenceFileId = "evidence_03",
                SystemId = "account_service",
                ResourceId = "acc_01",
                DataCategory = "USER_PROFILE",
                HasDirectIdentifiers = true
            },
            new()
            {
                EvidenceFileId = "evidence_04",
                SystemId = "identity_service",
                ResourceId = "tomb_01",
                DataCategory = "ACCOUNT_TOMBSTONE",
                HasDirectIdentifiers = true
            }
        };

        var steps = _stepBuilder.BuildIncompleteSteps(findings, identity, _ruleEngine.GetRequiredAction);

        Assert.NotEmpty(steps);

        // Check sequential numbering starting from 1
        for (int i = 0; i < steps.Count; i++)
        {
            Assert.Equal(i + 1, steps[i].StepNo);
        }

        // Verify Phase order: FINDING -> REMEDIATION -> VERIFICATION
        var phases = steps.Select(s => s.Phase).ToList();
        var findingCount = phases.Count(p => p == "FINDING");
        var remediationCount = phases.Count(p => p == "REMEDIATION");
        var verificationCount = phases.Count(p => p == "VERIFICATION");

        Assert.True(findingCount > 0);
        Assert.True(remediationCount > 0);
        Assert.True(verificationCount > 0);

        var firstRemediation = phases.IndexOf("REMEDIATION");
        var firstVerification = phases.IndexOf("VERIFICATION");

        Assert.True(firstRemediation > phases.LastIndexOf("FINDING"));
        Assert.True(firstVerification > phases.LastIndexOf("REMEDIATION"));

        // Verify Findings have evidence_id
        foreach (var step in steps.Where(s => s.Phase == "FINDING"))
        {
            Assert.False(string.IsNullOrEmpty(step.EvidenceId));
        }

        // Verify Remediation and future Verification have EMPTY evidence_id
        foreach (var step in steps.Where(s => s.Phase == "REMEDIATION"))
        {
            Assert.Equal("", step.EvidenceId);
        }
        foreach (var step in steps.Where(s => s.Phase == "VERIFICATION"))
        {
            Assert.Equal("", step.EvidenceId);
        }

        // Verify Remediation ordering: DISABLE_DATA_FLOW before BLOCK_RESTORE before DELETE_RECORD / REDACT_RECORD
        var remOps = steps.Where(s => s.Phase == "REMEDIATION").Select(s => s.Operation).ToList();
        var disableFlowIdx = remOps.IndexOf("DISABLE_DATA_FLOW");
        var blockRestoreIdx = remOps.IndexOf("BLOCK_RESTORE");
        var deleteRecordIdx = remOps.IndexOf("DELETE_RECORD");
        var redactRecordIdx = remOps.IndexOf("REDACT_RECORD");

        Assert.True(disableFlowIdx < blockRestoreIdx);
        Assert.True(blockRestoreIdx < deleteRecordIdx);
        Assert.True(deleteRecordIdx < redactRecordIdx);
    }

    [Fact]
    public void StepBuilder_CompleteCase_PreservesEvidenceIdInVerification()
    {
        var identity = new ResolvedIdentity
        {
            UserId = "user_200",
            CurrentName = "Complete User",
            AllEmails = { "complete@test.com" }
        };

        var verifiedRecords = new List<EvidenceRecord>
        {
            new()
            {
                EvidenceFileId = "evidence_54",
                SystemId = "account_service",
                ResourceId = "acc_200",
                DataCategory = "USER_PROFILE",
                HasDirectIdentifiers = false
            },
            new()
            {
                EvidenceFileId = "evidence_77",
                SystemId = "security_service",
                ResourceId = "sec_log_200",
                DataCategory = "SECURITY_AUDIT_LOG",
                HasDirectIdentifiers = false
            }
        };

        var steps = _stepBuilder.BuildCompleteSteps(verifiedRecords, identity, _ruleEngine.GetRequiredAction);

        Assert.Equal(2, steps.Count);
        Assert.All(steps, s => Assert.Equal("VERIFICATION", s.Phase));
        Assert.All(steps, s => Assert.False(string.IsNullOrEmpty(s.EvidenceId)));

        // Security audit log should be VERIFY_RECORD_PRESENT
        var secStep = steps.First(s => s.SystemId == "security_service");
        Assert.Equal("VERIFY_RECORD_PRESENT", secStep.Operation);
        Assert.Equal("PRESENT", secStep.State);
        Assert.Equal("evidence_77", secStep.EvidenceId);

        // Account service should be VERIFY_RECORD_ABSENT
        var accStep = steps.First(s => s.SystemId == "account_service");
        Assert.Equal("VERIFY_RECORD_ABSENT", accStep.Operation);
        Assert.Equal("ABSENT", accStep.State);
        Assert.Equal("evidence_54", accStep.EvidenceId);
    }
}

