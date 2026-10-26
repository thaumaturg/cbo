using Cbo.API.Cqrs;

namespace Cbo.API.Commands.Rounds;

/// <summary>
/// Replaces the answers of round <paramref name="RoundNumber"/>. <paramref name="NumberInMatch"/>,
/// <paramref name="TopicId"/> and <paramref name="IsOverrideMode"/> come from the request body and must match the round.
/// </summary>
public sealed record UpdateRoundCommand(
    Guid TournamentId,
    Guid MatchId,
    int RoundNumber,
    int NumberInMatch,
    Guid TopicId,
    bool IsOverrideMode,
    IReadOnlyList<GivenAnswer> Answers) : ICommand;
