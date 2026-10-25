using GhostDataAudit.Api.Models.Enums;
using GhostDataAudit.Api.Services.Interfaces;

namespace GhostDataAudit.Api.Services;

/// <summary>
/// Classifies whether a request_text authorizes data deletion.
/// Uses keyword-based multilingual detection. Non-deletion requests
/// (access, export, correction, marketing opt-out) return NOT_APPLICABLE.
/// </summary>
public class RequestClassifierService : IRequestClassifierService
{
    private readonly ILogger<RequestClassifierService> _logger;

    // Keywords that indicate the request is NOT a deletion request
    private static readonly string[] NonDeletionKeywords = new[]
    {
        // English
        "do not close", "don't close", "do not delete", "don't delete",
        "do not remove", "don't remove", "don't erase",
        "copy of", "export", "download my data",
        "access my", "reset the login", "reset my password",
        "marketing lists only", "opt-out", "opt out",
        "leave my profile", "leave my account",
        "still use it", "still want",
        "invoices for my records",
        "update my", "correct my", "correction",
        "I still use", "keep my account",
    };

    // Keywords that indicate the request IS a deletion request (across languages)
    private static readonly string[] DeletionKeywords = new[]
    {
        // English
        "delete", "deletion", "erase", "erased", "erasure",
        "remove", "removed", "close my account", "closed",
        "wipe", "purge", "destroy",
        // Spanish
        "eliminar", "eliminación", "borrar", "suprimir",
        // French
        "suppression", "supprimer", "effacer",
        // German
        "löschen", "löschung", "entfernen",
        // Portuguese
        "excluir", "exclusão", "apagar", "remover",
        // Italian
        "cancellare", "cancellazione", "eliminare",
        // Japanese
        "削除", "消去",
        // Korean
        "삭제", "제거",
        // Arabic
        "حذف", "إزالة",
        // Dutch
        "verwijderen", "verwijdering",
        // Polish
        "usunięcie", "usunąć",
        // Turkish
        "silme", "silinmesini",
        // Swedish
        "radering", "radera",
        // Norwegian
        "sletting", "slett",
        // Danish
        "sletning", "slet",
        // Czech
        "smazání", "smazat",
    };

    public RequestClassifierService(ILogger<RequestClassifierService> logger)
    {
        _logger = logger;
    }

    public Task<DeletionStatus> ClassifyRequestAsync(string requestText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(requestText))
        {
            _logger.LogWarning("Empty request text — classifying as NOT_APPLICABLE");
            return Task.FromResult(DeletionStatus.NOT_APPLICABLE);
        }

        var lower = requestText.ToLowerInvariant();

        // First check for explicit non-deletion indicators
        foreach (var nonDel in NonDeletionKeywords)
        {
            if (lower.Contains(nonDel, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Request classified as NOT_APPLICABLE (matched non-deletion keyword: '{Keyword}')", nonDel);
                return Task.FromResult(DeletionStatus.NOT_APPLICABLE);
            }
        }

        // Then check for deletion indicators
        foreach (var del in DeletionKeywords)
        {
            if (lower.Contains(del, StringComparison.OrdinalIgnoreCase))
            {
                // This is a deletion request — actual status will be determined by evidence
                return Task.FromResult(DeletionStatus.INCOMPLETE); // placeholder; orchestrator decides COMPLETE vs INCOMPLETE
            }
        }

        // If no clear signal, default to treating it as a deletion request (conservative)
        _logger.LogWarning("Request could not be clearly classified — defaulting to deletion: '{Snippet}'",
            requestText.Length > 80 ? requestText[..80] : requestText);
        return Task.FromResult(DeletionStatus.INCOMPLETE);
    }
}
