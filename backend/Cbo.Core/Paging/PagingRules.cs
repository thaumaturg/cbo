using Cbo.Results;

namespace Cbo.Core.Paging;

/// <summary>
/// Checks the paging parameters every list shares. A list handler adds its own filter errors and returns
/// <c>Failure.Invalid(...)</c> when there are any.
/// </summary>
public static class PagingRules
{
    public const string InvalidMessage = "Some of the list parameters are invalid.";

    public static List<FieldError> Validate<T>(int page, int pageSize, string? sort, string? search, SortKeys<T> sortKeys)
    {
        ArgumentNullException.ThrowIfNull(sortKeys);

        List<FieldError> errors = [];

        if (page < PagingDefaults.FirstPage)
            errors.Add(new FieldError("page", $"The page must be {PagingDefaults.FirstPage} or greater."));

        if (pageSize is < 1 or > PagingDefaults.MaxPageSize)
            errors.Add(new FieldError("pageSize", $"The page size must be between 1 and {PagingDefaults.MaxPageSize}."));

        if (!sortKeys.IsValid(sort))
            errors.Add(new FieldError("sort", sortKeys.UnknownKeyMessage));

        if (search?.Trim().Length > PagingDefaults.MaxSearchLength)
            errors.Add(new FieldError("search", $"The search can be at most {PagingDefaults.MaxSearchLength} characters."));

        return errors;
    }
}
