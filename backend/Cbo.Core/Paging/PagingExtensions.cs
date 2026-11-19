using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Paging;

public static class PagingExtensions
{
    /// <summary>
    /// Runs one page of an already filtered query: counts the matching rows, then sorts, skips, takes and projects.
    /// A page past the end returns no items with the right total count and skips the items query.
    /// Validate <paramref name="page"/>, <paramref name="pageSize"/> and <paramref name="sort"/> with
    /// <see cref="PagingRules"/> first.
    /// </summary>
    public static async Task<PageResponse<TOut>> ToPageAsync<T, TOut>(
        this IQueryable<T> filtered,
        SortKeys<T> sortKeys,
        string? sort,
        int page,
        int pageSize,
        Expression<Func<T, TOut>> projection,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filtered);
        ArgumentNullException.ThrowIfNull(sortKeys);
        ArgumentNullException.ThrowIfNull(projection);

        int totalCount = await filtered.CountAsync(cancellationToken);
        PageWindow window = PageWindow.For(page, pageSize, totalCount);

        List<TOut> items = window.IsPastEnd
            ? []
            : await sortKeys.Apply(filtered, sort)
                .Skip(window.Skip)
                .Take(pageSize)
                .Select(projection)
                .ToListAsync(cancellationToken);

        return new PageResponse<TOut>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }
}
