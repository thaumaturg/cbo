using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Cbo.Core.Domain;
using Cbo.Core.Paging;
using Cbo.Core.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.Core.Tournaments;

/// <summary>
/// The query string of <c>GET /Tournaments</c>. Every property is nullable, so a missing parameter gets its default
/// instead of a binding error. <see cref="Role"/> is singular because it repeats once per value.
/// </summary>
/// <remarks>
/// <c>[DefaultValue]</c>, <c>[Range]</c>, <c>[MaxLength]</c> and <c>[Description]</c> only document the parameters in OpenAPI.
/// The validator checks them.
/// </remarks>
public record GetTournamentsRequest
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
    [DefaultValue(TournamentListFilter.DefaultSort)]
    [Description(TournamentListFilter.SortDescription)]
    public string? Sort { get; set; }

    [FromQuery(Name = "search")]
    [MaxLength(PagingDefaults.MaxSearchLength)]
    public string? Search { get; set => field = RequestText.TrimToNull(value); }

    [FromQuery(Name = "stage")]
    public TournamentStageFilter? Stage { get; set; }

    [FromQuery(Name = "role")]
    public TournamentParticipantRole[]? Role { get; set; }
}
