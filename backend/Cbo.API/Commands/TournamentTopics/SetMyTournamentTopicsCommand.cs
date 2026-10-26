using Cbo.API.Cqrs;

namespace Cbo.API.Commands.TournamentTopics;

/// <summary>Replaces the current participant's topics in the tournament with <paramref name="Topics"/>.</summary>
public sealed record SetMyTournamentTopicsCommand(Guid TournamentId, IReadOnlyList<ChosenTopic> Topics) : ICommand;
