using GhostDataAudit.Api.Models;
using GhostDataAudit.Api.Services.Interfaces;

namespace GhostDataAudit.Api.Services;

public class RuleEngineService : IRuleEngineService
{
    private readonly ILogger<RuleEngineService> _logger;
    private readonly Dictionary<string, string> _categoryToAction = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;

    public RuleEngineService(ILogger<RuleEngineService> logger)
    {
        _logger = logger;
    }

    public void Initialize(IReadOnlyList<DeletionRule> rules)
    {
        _categoryToAction.Clear();
        foreach (var rule in rules)
        {
            _categoryToAction[rule.DataCategory] = rule.RequiredAction;
        }
        _initialized = true;
        _logger.LogInformation("RuleEngine initialized with {Count} rules", rules.Count);
    }

    public string GetRequiredAction(string dataCategory)
    {
        if (!_initialized)
            throw new InvalidOperationException("RuleEngineService.Initialize must be called first.");

        if (_categoryToAction.TryGetValue(dataCategory, out var action))
            return action;

        if (dataCategory.Equals("DATA_FLOW", StringComparison.OrdinalIgnoreCase))
            return "DISABLE";

        _logger.LogWarning("No rule found for data category '{Category}' — defaulting to DELETE", dataCategory);
        return "DELETE";
    }
}

