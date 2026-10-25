using GhostDataAudit.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GhostDataAudit.Tests;

public class DatasetIntegrationDiagnosticTests
{
    private readonly DataLoaderService _loader;

    public DatasetIntegrationDiagnosticTests()
    {
        _loader = new DataLoaderService(NullLogger<DataLoaderService>.Instance);
    }

    [Fact]
    public async Task ValidateAndDiagnoseDataset_FullDataset_HasExpectedRecordCountsAndNoErrors()
    {
        // Path relative to repository root
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var datasetDir = Path.Combine(repoRoot, "dataset");

        var report = await _loader.ValidateAndDiagnoseDatasetAsync(datasetDir);

        Assert.Equal(518, report.DeletionRequestsCount);
        Assert.Equal(32, report.SampleDeletionRequestsCount);
        Assert.Equal(850, report.UserIdentitiesCount);
        Assert.Equal(306, report.UserAuditLogCount);
        Assert.Equal(4011, report.SystemActivityCount);
        Assert.Equal(15, report.DeletionRulesCount);
        Assert.Equal(94, report.EvidenceFilesCount);

        Assert.Empty(report.ValidationErrors);
        Assert.Empty(report.MalformedRecords);
        Assert.Empty(report.MissingReferences);
        Assert.Empty(report.InaccessibleEvidenceFiles);
        Assert.True(report.IsValid);
    }
}

