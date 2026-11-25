using FluentValidation;

namespace Cbo.Core.Paging;

/// <summary>
/// Rules for the query parameters every list request shares. A missing parameter passes; the handler fills in its
/// default.
/// </summary>
internal static class PagingRequestRules
{
    public static IRuleBuilderOptions<T, int?> Page<T>(this IRuleBuilder<T, int?> rule) =>
        rule
            .GreaterThanOrEqualTo(PagingDefaults.FirstPage)
            .WithMessage($"The page must be {PagingDefaults.FirstPage} or greater.");

    public static IRuleBuilderOptions<T, int?> PageSize<T>(this IRuleBuilder<T, int?> rule) =>
        rule
            .InclusiveBetween(1, PagingDefaults.MaxPageSize)
            .WithMessage($"The page size must be between 1 and {PagingDefaults.MaxPageSize}.");

    public static IRuleBuilderOptions<T, string?> Sort<T, TRow>(this IRuleBuilder<T, string?> rule, SortKeys<TRow> sortKeys) =>
        rule
            .Must(sortKeys.IsValid)
            .WithMessage(sortKeys.UnknownKeyMessage);

    public static IRuleBuilderOptions<T, string?> Search<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .MaximumLength(PagingDefaults.MaxSearchLength)
            .WithMessage($"The search can be at most {PagingDefaults.MaxSearchLength} characters.");
}
