namespace GhostDataAudit.Api.Models;

public class UserIdentity
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string EmailHash { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

