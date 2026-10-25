using System.Text.Json;
using GhostDataAudit.Api.Models;
using GhostDataAudit.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GhostDataAudit.Tests;

public class OutputWriterServiceTests
{
    [Fact]
    public async Task WriteOutputCsvAsync_ValidResults_GeneratesValidCsvAndJson()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var writer = new OutputWriterService(NullLogger<OutputWriterService>.Instance);

            var sampleResults = new List<CaseResult>
            {
                new CaseResult
                {
                    CaseId = "case_02",
                    DeletionStatus = "COMPLETE",
                    Steps = new List<AuditStep>
                    {
                        new AuditStep
                        {
                            StepNo = 1,
                            Phase = "VERIFICATION",
                            SystemId = "account_service",
                            ResourceId = "account_002",
                            Operation = "VERIFY_RECORD_ABSENT",
                            State = "ABSENT",
                            EvidenceId = "evidence_54"
                        }
                    }
                },
                new CaseResult
                {
                    CaseId = "case_01",
                    DeletionStatus = "NOT_APPLICABLE",
                    Steps = new List<AuditStep>() // []
                }
            };

            await writer.WriteOutputCsvAsync(tempFile, sampleResults);

            var lines = await File.ReadAllLinesAsync(tempFile);
            Assert.True(lines.Length >= 3);
            Assert.Equal("case_id,deletion_status,steps_json", lines[0]);

            // Ensure case_01 comes before case_02
            Assert.StartsWith("case_01,NOT_APPLICABLE,\"[]\"", lines[1]);
            Assert.StartsWith("case_02,COMPLETE,", lines[2]);

            // Verify JSON parse of line 2
            var parts = lines[2].Split(new[] { ',' }, 3);
            var jsonRaw = parts[2].Trim('"').Replace("\"\"", "\"");
            var steps = JsonSerializer.Deserialize<List<AuditStep>>(jsonRaw);
            Assert.NotNull(steps);
            Assert.Single(steps);
            Assert.Equal("VERIFY_RECORD_ABSENT", steps[0].Operation);
            Assert.Equal(1, steps[0].StepNo);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
