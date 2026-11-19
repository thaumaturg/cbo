namespace Cbo.Core.Paging;

/// <summary>
/// The rows one page covers. <see cref="IsPastEnd"/> means the page has no rows, so the items query can be skipped.
/// A page past the end skips every row (<see cref="Skip"/> equals the total count).
/// </summary>
public readonly record struct PageWindow(int Skip, bool IsPastEnd)
{
    public static PageWindow For(int page, int pageSize, int totalCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, PagingDefaults.FirstPage);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

        // long, so a huge page number can't overflow
        long skip = (long)(page - 1) * pageSize;

        return skip >= totalCount
            ? new PageWindow(totalCount, IsPastEnd: true)
            : new PageWindow((int)skip, IsPastEnd: false);
    }
}
