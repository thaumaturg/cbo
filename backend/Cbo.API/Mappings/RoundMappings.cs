using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;

namespace Cbo.API.Mappings;

public static class RoundMappings
{
    /// <summary>
    /// Maps a Round to GetRoundDto.
    /// Requires: Topic (with Questions) and RoundAnswers to be loaded.
    /// </summary>
    public static GetRoundDto ToGetDto(this Round round, int topicPriorityIndex, string topicOwnerUsername)
    {
        return new GetRoundDto
        {
            Id = round.Id,
            NumberInMatch = round.NumberInMatch,
            IsOverrideMode = round.IsOverrideMode,
            TopicId = round.TopicId,
            TopicTitle = round.Topic.Title,
            TopicPriorityIndex = topicPriorityIndex,
            TopicOwnerUsername = topicOwnerUsername,
            MatchId = round.MatchId,
            Questions = round.Topic.Questions.OrderBy(q => q.QuestionNumber).Select(q => q.ToGetDto()).ToList(),
            RoundAnswers = round.RoundAnswers.Select(ra => ra.ToGetDto()).ToList()
        };
    }

    public static GetRoundAnswerDto ToGetDto(this RoundAnswer roundAnswer)
    {
        return new GetRoundAnswerDto
        {
            Id = roundAnswer.Id,
            IsAnswerAccepted = roundAnswer.IsAnswerAccepted,
            OverrideCost = roundAnswer.OverrideCost,
            QuestionId = roundAnswer.QuestionId,
            MatchParticipantId = roundAnswer.MatchParticipantId
        };
    }

    /// <summary>
    /// Creates a new Round entity from DTO (without answers).
    /// Caller is responsible for adding answers to RoundAnswers collection.
    /// </summary>
    public static Round ToNewRound(this CreateRoundWithAnswersDto dto, Guid matchId)
    {
        return new Round
        {
            NumberInMatch = dto.NumberInMatch,
            IsOverrideMode = dto.IsOverrideMode,
            TopicId = dto.TopicId,
            MatchId = matchId
        };
    }

    /// <summary>
    /// Creates a new RoundAnswer entity from DTO.
    /// When adding to a new Round's collection (before save), pass Guid.Empty - EF Core will set RoundId automatically.
    /// When adding to an existing Round, pass the Round's actual Id.
    /// </summary>
    public static RoundAnswer ToNewRoundAnswer(this CreateRoundAnswerDto dto, Guid roundId)
    {
        return new RoundAnswer
        {
            IsAnswerAccepted = dto.IsAnswerAccepted,
            OverrideCost = dto.OverrideCost,
            RoundId = roundId,
            QuestionId = dto.QuestionId,
            MatchParticipantId = dto.MatchParticipantId
        };
    }
}
