using Cbo.API.Contracts.Tournaments;
using Cbo.API.Cqrs;

namespace Cbo.API.Queries.Tournaments;

/// <summary>The tournaments the current user takes part in.</summary>
public sealed record GetTournamentsQuery : IQuery<List<TournamentResponse>>;
