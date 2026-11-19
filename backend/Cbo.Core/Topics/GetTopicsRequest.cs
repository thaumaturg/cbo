namespace Cbo.Core.Topics;

/// <summary>
/// The query string of <c>GET /Topics</c>. Every property is nullable, so a missing parameter gets its default
/// instead of a binding error.
/// </summary>
public record GetTopicsRequest
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
    public string? Search { get; set; }
    public bool? Played { get; set; }
    public bool? IsAuthor { get; set; }
}
