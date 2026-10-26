namespace Cbo.API.Commands.Rounds;

/// <summary>
/// A match participant's answer to one question of a round. In standard mode <paramref name="IsAnswerAccepted"/> is
/// set; in override mode <paramref name="OverrideCost"/> is.
/// </summary>
public sealed record GivenAnswer(
    Guid QuestionId,
    Guid MatchParticipantId,
    bool? IsAnswerAccepted,
    int? OverrideCost);
