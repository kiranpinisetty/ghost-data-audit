using Microsoft.AspNetCore.Mvc;
using GhostDataAudit.Api.Models;
using GhostDataAudit.Api.Services.Interfaces;

namespace GhostDataAudit.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly IAuditOrchestratorService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditOrchestratorService auditService, ILogger<AuditController> logger)
    {
        _auditService = auditService;
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

    [HttpPost("audit-case")]
    public async Task<ActionResult<CaseResult>> AuditCase([FromBody] DeletionRequest request)
    {
        _logger.LogInformation("Received audit request for case {CaseId}", request.CaseId);
        var result = await _auditService.AuditCaseAsync(request);
        return Ok(result);
    }
}

