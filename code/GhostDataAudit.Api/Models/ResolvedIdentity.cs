namespace GhostDataAudit.Api.Models;

/// <summary>
/// All current and historical identifiers for a resolved user.
/// Used by the evidence search to find every trace of the user across systems.
/// </summary>
public class ResolvedIdentity
{
    public string UserId { get; set; } = string.Empty;
    public string CurrentName { get; set; } = string.Empty;
    public HashSet<string> AllEmails { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> AllPhones { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> AllEmailHashes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns true if the given value matches any known identifier for this user.
    /// Case-insensitive for emails and hashes.
    /// </summary>
    public bool MatchesIdentifier(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        var trimmed = value.Trim();
        if (string.IsNullOrEmpty(trimmed)) return false;

        return UserId.Equals(trimmed, StringComparison.OrdinalIgnoreCase)
            || AllEmails.Contains(trimmed)
            || AllPhones.Contains(trimmed)
            || AllEmailHashes.Contains(trimmed);
    }
}

