using System.Text.Json.Serialization;

namespace GhostDataAudit.Api.Models;

public class AuditStep
{
    [JsonPropertyName("step_no")]
    public int StepNo { get; set; }

    [JsonPropertyName("phase")]
    public string Phase { get; set; } = string.Empty;

    [JsonPropertyName("system_id")]
    public string SystemId { get; set; } = string.Empty;

    [JsonPropertyName("resource_id")]
    public string ResourceId { get; set; } = string.Empty;

    [JsonPropertyName("operation")]
    public string Operation { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("evidence_id")]
    public string EvidenceId { get; set; } = string.Empty;
}

