using Cbo.API.Cqrs;
using Cbo.API.Models.DTO;

namespace Cbo.API.Queries.TournamentTopics;

/// <summary>Every participant's topics in the tournament, grouped by participant and ordered by priority.</summary>
public sealed record GetAllTournamentTopicsQuery(Guid TournamentId) : IQuery<List<GetTournamentTopicDto>>;
