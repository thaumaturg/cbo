using Cbo.Results;

namespace Cbo.Core.Topics;

/// <summary>
/// The co-author checks that need the accounts, run by the create and update handlers after
/// <see cref="CoAuthorLookup"/>. The request validators have already made sure each username is given once.
/// </summary>
internal static class CoAuthorRules
{
    /// <summary>
    /// Each co-author must have an account, and it can't be the owner's. Returns field errors keyed by
    /// <c>authors[i].username</c>; empty when every co-author is fine.
    /// </summary>
    public static IReadOnlyList<FieldError> CheckAccounts(IReadOnlyList<CoAuthorAccount> coAuthors, Guid ownerId)
    {
        List<FieldError> errors = [];

        for (int i = 0; i < coAuthors.Count; i++)
        {
            CoAuthorAccount coAuthor = coAuthors[i];
            string field = $"authors[{i}].username";

            if (coAuthor.UserId is null)
                errors.Add(new FieldError(field, $"There's no account with the username '{coAuthor.Username}'."));
            else if (coAuthor.UserId == ownerId)
                errors.Add(new FieldError(field, "The owner can't be added as a co-author."));
        }

        return errors;
    }
}
