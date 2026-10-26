using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;

namespace Cbo.API.Mappings;

public static class TopicMappings
{
    public static GetQuestionDto ToGetDto(this Question question)
    {
        return new GetQuestionDto
        {
            Id = question.Id,
            QuestionNumber = question.QuestionNumber,
            CostPositive = question.CostPositive,
            CostNegative = question.CostNegative,
            Text = question.Text,
            Answer = question.Answer,
            Comment = question.Comment,
            TopicId = question.TopicId
        };
    }
}
