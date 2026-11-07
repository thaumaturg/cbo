using Cbo.Core.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Topics;

/// <summary>
/// Resolves the co-author usernames of a create or update topic request to accounts, ready for
/// <see cref="ITopicValidationService.ValidateCoAuthors"/>.
/// </summary>
internal static class CoAuthorLookup
{
    /// <summary>
    /// Looks every username up in one query, keeping the request's order. Usernames compare the way Identity
    /// compares them (normalized), so "Bob" and "bob" are the same account.
    /// </summary>
    public static async Task<List<CoAuthorValidationModel>> LookUpAsync(
        CboDbContext dbContext,
        ILookupNormalizer lookupNormalizer,
        IReadOnlyList<string> usernames,
        CancellationToken cancellationToken)
    {
        List<string?> normalizedUsernames = usernames
            .Select(u => string.IsNullOrWhiteSpace(u) ? null : lookupNormalizer.NormalizeName(u))
            .ToList();

        List<string> namesToFind = normalizedUsernames.OfType<string>().Distinct().ToList();
        Dictionary<string, Guid> userIdsByNormalizedUsername = namesToFind.Count == 0
            ? []
            : await dbContext.ApplicationUsers
                .AsNoTracking()
                .Where(u => namesToFind.Contains(u.NormalizedUserName!))
                .ToDictionaryAsync(u => u.NormalizedUserName!, u => u.Id, cancellationToken);

        return usernames
            .Select((username, i) =>
            {
                string? normalizedUsername = normalizedUsernames[i];
                Guid? userId = normalizedUsername is not null &&
                    userIdsByNormalizedUsername.TryGetValue(normalizedUsername, out Guid id)
                        ? id
                        : null;
                return new CoAuthorValidationModel(username, normalizedUsername, userId);
            })
            .ToList();
    }
}
