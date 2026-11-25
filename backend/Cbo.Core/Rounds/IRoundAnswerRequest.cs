namespace Cbo.Core.Rounds;

/// <summary>
/// The fields of an answer in a create or update round request, so <see cref="RoundAnswerRequestValidator"/> checks
/// them both the same way.
/// </summary>
public interface IRoundAnswerRequest
{
    bool? IsAnswerAccepted { get; }
    int? OverrideCost { get; }
    Guid QuestionId { get; }
    Guid MatchParticipantId { get; }
}
