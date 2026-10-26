using System.Linq.Expressions;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;

namespace Cbo.API.Mappings;

/// <inheritdoc cref="TournamentProjections"/>
public static class QuestionProjections
{
    public static Expression<Func<Question, GetQuestionDto>> ToGetDto() =>
        q => new GetQuestionDto
        {
            Id = q.Id,
            QuestionNumber = q.QuestionNumber,
            CostPositive = q.CostPositive,
            CostNegative = q.CostNegative,
            Text = q.Text,
            Answer = q.Answer,
            Comment = q.Comment,
            TopicId = q.TopicId,
        };
}
