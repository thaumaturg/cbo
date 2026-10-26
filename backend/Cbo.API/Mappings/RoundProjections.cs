using System.Linq.Expressions;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;

namespace Cbo.API.Mappings;

/// <inheritdoc cref="TournamentProjections"/>
public static class RoundProjections
{
    /// <summary>
    /// Questions come ordered by number, answers by id. The topic's priority and owner are those of the tournament
    /// topic that brought it into the match's tournament.
    /// </summary>
    public static Expression<Func<Round, GetRoundDto>> ToGetDto()
    {
        Expression<Func<Question, GetQuestionDto>> toQuestionDto = QuestionProjections.ToGetDto();

        return r => new GetRoundDto
        {
            Id = r.Id,
            NumberInMatch = r.NumberInMatch,
            IsOverrideMode = r.IsOverrideMode,
            TopicId = r.TopicId,
            TopicTitle = r.Topic!.Title,
            TopicPriorityIndex = r.Topic.TournamentTopics
                .Where(tt => tt.TournamentId == r.Match!.TournamentId)
                .Select(tt => tt.PriorityIndex)
                .FirstOrDefault(),
            TopicOwnerUsername = r.Topic.TournamentTopics
                .Where(tt => tt.TournamentId == r.Match!.TournamentId)
                .Select(tt => tt.TournamentParticipant!.ApplicationUser!.UserName)
                .FirstOrDefault() ?? string.Empty,
            MatchId = r.MatchId,
            Questions = r.Topic.Questions
                .AsQueryable()
                .OrderBy(q => q.QuestionNumber)
                .Select(toQuestionDto)
                .ToList(),
            RoundAnswers = r.RoundAnswers
                .OrderBy(ra => ra.Id)
                .Select(ra => new GetRoundAnswerDto
                {
                    Id = ra.Id,
                    IsAnswerAccepted = ra.IsAnswerAccepted,
                    OverrideCost = ra.OverrideCost,
                    QuestionId = ra.QuestionId,
                    MatchParticipantId = ra.MatchParticipantId,
                })
                .ToList(),
        };
    }
}
