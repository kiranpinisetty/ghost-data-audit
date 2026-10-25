using GhostDataAudit.Api.Models;
using GhostDataAudit.Api.Services.Interfaces;

namespace GhostDataAudit.Api.Services;

public class IdentityResolverService : IIdentityResolverService
{
    private readonly ILogger<IdentityResolverService> _logger;

    // Index: lowercase email → user_id
    private readonly Dictionary<string, string> _emailToUserId = new(StringComparer.OrdinalIgnoreCase);
    // Index: user_id → UserIdentity
    private readonly Dictionary<string, UserIdentity> _userById = new(StringComparer.OrdinalIgnoreCase);
    // Index: user_id → list of audit entries
    private readonly Dictionary<string, List<UserAuditEntry>> _auditByUser = new(StringComparer.OrdinalIgnoreCase);

    private bool _initialized;

    public IdentityResolverService(ILogger<IdentityResolverService> logger)
    {
        _logger = logger;
    }

    public void Initialize(IReadOnlyList<UserIdentity> users, IReadOnlyList<UserAuditEntry> auditLog)
    {
        _emailToUserId.Clear();
        _userById.Clear();
        _auditByUser.Clear();

        foreach (var user in users)
        {
            _userById[user.UserId] = user;
            _emailToUserId[user.Email] = user.UserId;
        }

        foreach (var entry in auditLog)
        {
            if (!_auditByUser.TryGetValue(entry.UserId, out var list))
            {
                list = new List<UserAuditEntry>();
                _auditByUser[entry.UserId] = list;
            }
            list.Add(entry);

            // Index historical emails → user_id
            if (entry.FieldName.Equals("email", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(entry.OldValue))
                    _emailToUserId.TryAdd(entry.OldValue, entry.UserId);
                if (!string.IsNullOrEmpty(entry.NewValue))
                    _emailToUserId.TryAdd(entry.NewValue, entry.UserId);
            }
        }

        _initialized = true;
        _logger.LogInformation("IdentityResolver initialized with {Users} users and {AuditEntries} audit entries",
            users.Count, auditLog.Count);
    }

    public ResolvedIdentity? Resolve(string email)
    {
        if (!_initialized)
            throw new InvalidOperationException("IdentityResolverService.Initialize must be called first.");

        if (string.IsNullOrWhiteSpace(email))
            return null;

        if (!_emailToUserId.TryGetValue(email, out var userId))
        {
            _logger.LogWarning("Cannot resolve email '{Email}' to any known user", email);
            return null;
        }

        if (!_userById.TryGetValue(userId, out var user))
        {
            _logger.LogWarning("User '{UserId}' found in email index but not in user identities", userId);
            return null;
        }

        var identity = new ResolvedIdentity
        {
            UserId = userId,
            CurrentName = user.Name
        };

        // Add current identifiers
        if (!string.IsNullOrEmpty(user.Email))
            identity.AllEmails.Add(user.Email);
        if (!string.IsNullOrEmpty(user.Phone))
            identity.AllPhones.Add(user.Phone);
        if (!string.IsNullOrEmpty(user.EmailHash))
            identity.AllEmailHashes.Add(user.EmailHash);

        // Add historical identifiers from audit log
        if (_auditByUser.TryGetValue(userId, out var entries))
        {
            foreach (var entry in entries)
            {
                switch (entry.FieldName.ToLowerInvariant())
                {
                    case "email":
                        if (!string.IsNullOrEmpty(entry.OldValue)) identity.AllEmails.Add(entry.OldValue);
                        if (!string.IsNullOrEmpty(entry.NewValue)) identity.AllEmails.Add(entry.NewValue);
                        break;
                    case "phone":
                        if (!string.IsNullOrEmpty(entry.OldValue)) identity.AllPhones.Add(entry.OldValue);
                        if (!string.IsNullOrEmpty(entry.NewValue)) identity.AllPhones.Add(entry.NewValue);
                        break;
                    case "email_hash":
                        if (!string.IsNullOrEmpty(entry.OldValue)) identity.AllEmailHashes.Add(entry.OldValue);
                        if (!string.IsNullOrEmpty(entry.NewValue)) identity.AllEmailHashes.Add(entry.NewValue);
                        break;
                }
            }
        }

        return identity;
    }
}
