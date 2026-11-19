using Cbo.Core.Cqrs;
using Cbo.Core.Domain;
using Cbo.Core.Paging;

namespace Cbo.Core.Tournaments;

/// <summary>
/// One page of the tournaments the current user takes part in.
/// A null <paramref name="Search"/> or <paramref name="Stage"/>, or empty <paramref name="Roles"/>, means no filter.
/// </summary>
public sealed record GetTournamentsQuery(
    int Page,
    int PageSize,
    string? Sort,
    string? Search,
    TournamentStageFilter? Stage,
    IReadOnlyList<TournamentParticipantRole> Roles) : IQuery<PageResponse<TournamentResponse>>;
