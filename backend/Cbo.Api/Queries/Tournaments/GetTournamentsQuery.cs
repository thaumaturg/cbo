using Cbo.Api.Contracts.Tournaments;
using Cbo.Api.Cqrs;

namespace Cbo.Api.Queries.Tournaments;

/// <summary>The tournaments the current user takes part in.</summary>
public sealed record GetTournamentsQuery : IQuery<List<TournamentResponse>>;
