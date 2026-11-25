using Cbo.Core.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Topics;

/// <summary>
/// A co-author username of a create or update topic request, with the account it belongs to.
/// <paramref name="UserId"/> is null when no account has the username.
/// </summary>
internal sealed record CoAuthorAccount(string Username, Guid? UserId);

/// <summary>
/// Resolves the co-author usernames of a create or update topic request to accounts, ready for
/// <see cref="CoAuthorRules.CheckAccounts"/>.
/// </summary>
internal static class CoAuthorLookup
{
    /// <summary>
    /// Looks every username up in one query, keeping the request's order. Usernames compare the way Identity
    /// compares them (normalized), so "Bob" and "bob" are the same account.
    /// </summary>
    public static async Task<List<CoAuthorAccount>> LookUpAsync(
        CboDbContext dbContext,
        ILookupNormalizer lookupNormalizer,
        IReadOnlyList<string> usernames,
        CancellationToken cancellationToken)
    {
        // The request validators make sure every username is given, so none normalizes to null
        List<string> normalizedUsernames = usernames.Select(u => lookupNormalizer.NormalizeName(u)!).ToList();

        List<string> namesToFind = normalizedUsernames.Distinct().ToList();
        Dictionary<string, Guid> userIdsByNormalizedUsername = namesToFind.Count == 0
            ? []
            : await dbContext.ApplicationUsers
                .AsNoTracking()
                .Where(u => namesToFind.Contains(u.NormalizedUserName!))
                .ToDictionaryAsync(u => u.NormalizedUserName!, u => u.Id, cancellationToken);

        return usernames
            .Select((username, i) =>
            {
                Guid? userId = userIdsByNormalizedUsername.TryGetValue(normalizedUsernames[i], out Guid id) ? id : null;
                return new CoAuthorAccount(username, userId);
            })
            .ToList();
    }
}
