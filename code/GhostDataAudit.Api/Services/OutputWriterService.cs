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

    public Task WriteOutputCsvAsync(string outputPath, IEnumerable<CaseResult> results, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Writing output CSV to {OutputPath}", outputPath);
        return Task.CompletedTask;
    }
}

