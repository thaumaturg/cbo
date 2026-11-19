using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Cbo.Core.Paging;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.Core.Topics;

/// <summary>
/// The query string of <c>GET /Topics</c>. Every property is nullable, so a missing parameter gets its default
/// instead of a binding error.
/// </summary>
/// <remarks>
/// <c>[DefaultValue]</c>, <c>[Range]</c> and <c>[Description]</c> only document the parameters in OpenAPI.
/// The handler validates them.
/// </remarks>
public record GetTopicsRequest
{
    [FromQuery(Name = "page")]
    [DefaultValue(PagingDefaults.FirstPage)]
    [Range(PagingDefaults.FirstPage, int.MaxValue)]
    public int? Page { get; set; }

    [FromQuery(Name = "pageSize")]
    [DefaultValue(PagingDefaults.DefaultPageSize)]
    [Range(1, PagingDefaults.MaxPageSize)]
    public int? PageSize { get; set; }

    [FromQuery(Name = "sort")]
    [DefaultValue(TopicListFilter.DefaultSort)]
    [Description(TopicListFilter.SortDescription)]
    public string? Sort { get; set; }

    [FromQuery(Name = "search")]
    public string? Search { get; set; }

    [FromQuery(Name = "played")]
    public bool? Played { get; set; }

    [FromQuery(Name = "isAuthor")]
    public bool? IsAuthor { get; set; }
}
