using Cbo.API.Models.DTO;

namespace Cbo.API.Services;

public class RoundService : IRoundService
{
    public string? ValidateRoundAnswers(List<CreateRoundAnswerDto> answers)
    {
        var positiveAnswersByQuestion = answers
            .Where(a => a.IsAnswerAccepted)
            .GroupBy(a => a.QuestionId)
            .Where(g => g.Count() > 1)
            .ToList();

        if (positiveAnswersByQuestion.Count > 0)
        {
            int questionId = positiveAnswersByQuestion.First().Key;
            return $"Question {questionId} has multiple correct answers. Only one correct answer is allowed per question.";
        }

        return null;
    }
}
