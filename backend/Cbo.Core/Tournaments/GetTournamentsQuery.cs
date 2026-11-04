using Cbo.Core.Cqrs;

namespace Cbo.Core.Tournaments;

/// <summary>The tournaments the current user takes part in.</summary>
public sealed record GetTournamentsQuery : IQuery<List<TournamentResponse>>;
