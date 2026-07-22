using Cbo.API.Models.Constants;

namespace Cbo.API.Services;

/// <summary>
/// Question fields shared by create and update DTOs, in the shape the validator needs.
/// </summary>
public record QuestionValidationModel(
    int QuestionNumber,
    int CostPositive,
    int CostNegative,
    string? Text,
    string? Answer);

public interface ITopicValidationService
{
    /// <summary>
    /// Validates a topic's question collection against the constraints in <see cref="DefaultSettings"/>.
    /// Returns field-level errors keyed by JSON property path; empty when valid.
    /// </summary>
    Dictionary<string, string[]> ValidateQuestions(IReadOnlyList<QuestionValidationModel> questions);
}

public class TopicValidationService : ITopicValidationService
{
    public Dictionary<string, string[]> ValidateQuestions(IReadOnlyList<QuestionValidationModel> questions)
    {
        var errors = new Dictionary<string, List<string>>();

        ValidateQuestionCount(questions, errors);
        ValidateQuestionNumbering(questions, errors);

        for (int i = 0; i < questions.Count; i++)
        {
            ValidateQuestionFields(questions[i], i, errors);
        }

        return errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
    }

    private static void ValidateQuestionCount(
        IReadOnlyList<QuestionValidationModel> questions,
        Dictionary<string, List<string>> errors)
    {
        if (questions.Count < DefaultSettings.QuestionsPerTopicMin)
        {
            AddError(errors, "questions",
                $"A topic must have at least {DefaultSettings.QuestionsPerTopicMin} question(s).");
        }
        else if (questions.Count > DefaultSettings.QuestionsPerTopicMax)
        {
            AddError(errors, "questions",
                $"A topic cannot have more than {DefaultSettings.QuestionsPerTopicMax} questions.");
        }
    }

    private static void ValidateQuestionNumbering(
        IReadOnlyList<QuestionValidationModel> questions,
        Dictionary<string, List<string>> errors)
    {
        bool isSequential = questions
            .Select(q => q.QuestionNumber)
            .OrderBy(n => n)
            .SequenceEqual(Enumerable.Range(1, questions.Count));

        if (!isSequential)
        {
            AddError(errors, "questions",
                $"Question numbers must be unique and sequential from 1 to {questions.Count}.");
        }
    }

    private static void ValidateQuestionFields(
        QuestionValidationModel question,
        int index,
        Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(question.Text))
            AddError(errors, $"questions[{index}].text", "Question text is required.");

        if (string.IsNullOrWhiteSpace(question.Answer))
            AddError(errors, $"questions[{index}].answer", "Answer is required.");

        if (question.CostPositive is < DefaultSettings.QuestionsCostMin or > DefaultSettings.QuestionsCostMax)
        {
            AddError(errors, $"questions[{index}].costPositive",
                $"CostPositive must be between {DefaultSettings.QuestionsCostMin} and {DefaultSettings.QuestionsCostMax}.");
        }

        if (question.CostNegative is < DefaultSettings.QuestionsCostMin or > DefaultSettings.QuestionsCostMax)
        {
            AddError(errors, $"questions[{index}].costNegative",
                $"CostNegative must be between {DefaultSettings.QuestionsCostMin} and {DefaultSettings.QuestionsCostMax}.");
        }
    }

    private static void AddError(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out List<string>? messages))
        {
            messages = [];
            errors[key] = messages;
        }

        messages.Add(message);
    }
}
