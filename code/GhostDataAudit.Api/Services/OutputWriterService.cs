using System.Text;
using System.Text.Json;
using GhostDataAudit.Api.Models;
using GhostDataAudit.Api.Services.Interfaces;

namespace GhostDataAudit.Api.Services;

public class OutputWriterService : IOutputWriterService
{
    private readonly ILogger<OutputWriterService> _logger;

    public OutputWriterService(ILogger<OutputWriterService> logger)
    {
        _logger = logger;
    }

    public async Task WriteOutputCsvAsync(string outputPath, IEnumerable<CaseResult> results, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Writing output CSV to {OutputPath}", outputPath);

        var sortedResults = results.OrderBy(r => GetCaseNumericId(r.CaseId)).ToList();
        var sb = new StringBuilder();

        // Exact required header
        sb.AppendLine("case_id,deletion_status,steps_json");

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        foreach (var result in sortedResults)
        {
            var jsonString = JsonSerializer.Serialize(result.Steps, jsonOptions);

            // In CSV, JSON string must be enclosed in quotes, with internal double-quotes doubled ("")
            var escapedJson = "\"" + jsonString.Replace("\"", "\"\"") + "\"";

            sb.AppendLine($"{result.CaseId},{result.DeletionStatus},{escapedJson}");
        }

        // Write without UTF-8 BOM so the first header is strictly 'case_id'
        await File.WriteAllTextAsync(outputPath, sb.ToString(), new UTF8Encoding(false), cancellationToken);
        _logger.LogInformation("Successfully wrote {Count} cases to {OutputPath}", sortedResults.Count, outputPath);
    }

    private static int GetCaseNumericId(string caseId)
    {
        if (string.IsNullOrEmpty(caseId)) return 0;
        var parts = caseId.Split('_');
        if (parts.Length > 1 && int.TryParse(parts[1], out var num))
            return num;
        return 0;
    }
}
