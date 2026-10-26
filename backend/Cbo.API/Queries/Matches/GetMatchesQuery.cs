using Cbo.API.Cqrs;
using Cbo.API.Models.DTO;

namespace Cbo.API.Queries.Matches;

/// <summary>The tournament's matches, ordered by their number in the tournament.</summary>
public sealed record GetMatchesQuery(Guid TournamentId) : IQuery<List<GetMatchDto>>;
