using GhostDataAudit.Api.Models;

namespace GhostDataAudit.Api.Services.Interfaces;

public interface IOutputWriterService
{
    Task WriteOutputCsvAsync(string outputPath, IEnumerable<CaseResult> results, CancellationToken cancellationToken = default);
}

