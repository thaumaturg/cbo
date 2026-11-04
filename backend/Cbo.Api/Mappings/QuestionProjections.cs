using System.Linq.Expressions;
using Cbo.Api.Contracts.Topics;
using Cbo.Api.Models.Domain;

namespace Cbo.Api.Mappings;

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
