using GhostDataAudit.Api.Models;
using GhostDataAudit.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GhostDataAudit.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly IAuditOrchestratorService _auditService;
    private readonly IDataLoaderService _dataLoaderService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuditController> _logger;

    public AuditController(
        IAuditOrchestratorService auditService,
        IDataLoaderService dataLoaderService,
        IConfiguration configuration,
        ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _dataLoaderService = dataLoaderService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            Status = "Ready",
            Timestamp = DateTime.UtcNow,
            Service = "Ghost Data Still Exists Audit Agent"
        });
    }

    [HttpGet("diagnostics")]
    public async Task<ActionResult<DatasetDiagnosticReport>> GetDatasetDiagnostics([FromQuery] string? datasetPath, CancellationToken cancellationToken)
    {
        var basePath = datasetPath
            ?? _configuration["Dataset:Path"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "../../dataset");

        _logger.LogInformation("Running dataset diagnostics on path: {Path}", basePath);
        var report = await _dataLoaderService.ValidateAndDiagnoseDatasetAsync(basePath, cancellationToken);
        return Ok(report);
    }

    [HttpPost("audit-case")]
    public async Task<ActionResult<CaseResult>> AuditCase([FromBody] DeletionRequest request)
    {
        _logger.LogInformation("Received audit request for case {CaseId}", request.CaseId);
        var result = await _auditService.AuditCaseAsync(request);
        return Ok(result);
    }
}
