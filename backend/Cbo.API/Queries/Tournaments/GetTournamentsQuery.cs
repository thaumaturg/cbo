using Cbo.API.Cqrs;
using Cbo.API.Models.DTO;

namespace Cbo.API.Queries.Tournaments;

/// <summary>The tournaments the current user takes part in.</summary>
public sealed record GetTournamentsQuery : IQuery<List<GetTournamentDto>>;
