using Cbo.API.Cqrs;

namespace Cbo.API.Commands.Rounds;

/// <summary>Plays a round of the match. The round is addressed by <paramref name="NumberInMatch"/>, so no id is returned.</summary>
public sealed record CreateRoundCommand(
    Guid TournamentId,
    Guid MatchId,
    int NumberInMatch,
    Guid TopicId,
    bool IsOverrideMode,
    IReadOnlyList<GivenAnswer> Answers) : ICommand;
