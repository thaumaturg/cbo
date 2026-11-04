using Cbo.Api.Cqrs;

namespace Cbo.Api.Commands.TournamentTopics;

/// <summary>Replaces the current participant's topics in the tournament with <paramref name="Topics"/>.</summary>
public sealed record SetMyTournamentTopicsCommand(Guid TournamentId, IReadOnlyList<ChosenTopic> Topics) : ICommand;
