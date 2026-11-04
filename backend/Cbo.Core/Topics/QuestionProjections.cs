using System.Linq.Expressions;
using Cbo.Core.Models.Domain;
using Cbo.Core.Tournaments;

namespace Cbo.Core.Topics;

/// <inheritdoc cref="TournamentProjections"/>
public static class QuestionProjections
{
    public static Expression<Func<Question, QuestionResponse>> ToResponse() =>
        q => new QuestionResponse
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
