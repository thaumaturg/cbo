using System.Linq.Expressions;

namespace Cbo.Core.Paging;

/// <summary>
/// The fixed set of keys a list can be sorted by (ADR-0008). A <c>sort</c> string is one key, matched exactly,
/// with an optional leading <c>-</c> for descending, for example <c>-createdAt</c>.
/// </summary>
public sealed class SortKeys<T>
{
    private const char DescendingPrefix = '-';

    private readonly Dictionary<string, SortKey> _keys = new(StringComparer.Ordinal);
    private readonly List<string> _keyNames = [];
    private readonly string _defaultSort;
    private readonly Expression<Func<T, Guid>> _tieBreaker;

    public SortKeys(string defaultSort, Expression<Func<T, Guid>> tieBreaker)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultSort);
        ArgumentNullException.ThrowIfNull(tieBreaker);

        _defaultSort = defaultSort;
        _tieBreaker = tieBreaker;
    }

    public SortKeys<T> Add<TKey>(string key, Expression<Func<T, TKey>> selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(selector);

        _keys.Add(key, new SortKey(q => q.OrderBy(selector), q => q.OrderByDescending(selector)));
        _keyNames.Add(key);
        return this;
    }

    /// <summary>The field message for an invalid <c>sort</c>, listing the allowed keys.</summary>
    public string UnknownKeyMessage => $"Unknown sort key. Allowed: {string.Join(", ", _keyNames)}.";

    /// <summary>
    /// True for one allowed key with an optional leading <c>-</c>, and for a missing or blank sort,
    /// which means the default sort.
    /// </summary>
    public bool IsValid(string? sort)
    {
        return string.IsNullOrWhiteSpace(sort) || TryParse(sort, out _, out _);
    }

    /// <summary>
    /// Orders the query by the sort's key, then by the tie-breaker ascending. A missing or blank sort applies the default.
    /// </summary>
    /// <exception cref="ArgumentException">The sort is not valid; check <see cref="IsValid"/> first.</exception>
    public IOrderedQueryable<T> Apply(IQueryable<T> query, string? sort)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(sort))
        {
            if (!TryParse(_defaultSort, out SortKey defaultKey, out bool defaultDescending))
                throw new InvalidOperationException($"The default sort '{_defaultSort}' is not a registered key.");

            return Order(query, defaultKey, defaultDescending);
        }

        if (!TryParse(sort, out SortKey sortKey, out bool descending))
            throw new ArgumentException($"Unknown sort '{sort}'.", nameof(sort));

        return Order(query, sortKey, descending);
    }

    private IOrderedQueryable<T> Order(IQueryable<T> query, SortKey sortKey, bool descending)
    {
        IOrderedQueryable<T> ordered = descending ? sortKey.OrderDescending(query) : sortKey.OrderAscending(query);
        return ordered.ThenBy(_tieBreaker);
    }

    private bool TryParse(string sort, out SortKey sortKey, out bool descending)
    {
        descending = sort.StartsWith(DescendingPrefix);
        string key = descending ? sort[1..] : sort;

        return _keys.TryGetValue(key, out sortKey!);
    }

    private sealed record SortKey(
        Func<IQueryable<T>, IOrderedQueryable<T>> OrderAscending,
        Func<IQueryable<T>, IOrderedQueryable<T>> OrderDescending);
}
