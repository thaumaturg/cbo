namespace Cbo.Core.Paging;

/// <summary>
/// Builds <c>LIKE</c>/<c>ILIKE</c> patterns from user input, so <c>%</c>, <c>_</c> and <c>\</c> match themselves.
/// Pass <see cref="EscapeCharacter"/> to <c>EF.Functions.ILike(column, pattern, LikePattern.EscapeCharacter)</c>.
/// </summary>
public static class LikePattern
{
    public const string EscapeCharacter = "\\";

    /// <summary>Returns <c>%{term}%</c> with the term trimmed and its wildcards escaped.</summary>
    public static string Contains(string term)
    {
        ArgumentNullException.ThrowIfNull(term);

        string escaped = term.Trim()
            .Replace(EscapeCharacter, EscapeCharacter + EscapeCharacter)
            .Replace("%", EscapeCharacter + "%")
            .Replace("_", EscapeCharacter + "_");

        return $"%{escaped}%";
    }
}
