using Cbo.API.Models.Constants;
using Cbo.Results;

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
    /// Returns field errors keyed by camelCase JSON path (for example <c>questions[0].text</c>); empty when valid.
    /// </summary>
    List<FieldError> ValidateQuestions(IReadOnlyList<QuestionValidationModel> questions);
}

public class TopicValidationService : ITopicValidationService
{
    public List<FieldError> ValidateQuestions(IReadOnlyList<QuestionValidationModel> questions)
    {
        List<FieldError> errors = [];

        ValidateQuestionCount(questions, errors);
        ValidateQuestionNumbering(questions, errors);

        for (int i = 0; i < questions.Count; i++)
        {
            ValidateQuestionFields(questions[i], i, errors);
        }

        return errors;
    }

    private static void ValidateQuestionCount(
        IReadOnlyList<QuestionValidationModel> questions,
        List<FieldError> errors)
    {
        if (questions.Count < DefaultSettings.QuestionsPerTopicMin)
        {
            errors.Add(new FieldError("questions",
                $"A topic must have at least {DefaultSettings.QuestionsPerTopicMin} question(s)."));
        }
        else if (questions.Count > DefaultSettings.QuestionsPerTopicMax)
        {
            errors.Add(new FieldError("questions",
                $"A topic cannot have more than {DefaultSettings.QuestionsPerTopicMax} questions."));
        }
    }

    private static void ValidateQuestionNumbering(
        IReadOnlyList<QuestionValidationModel> questions,
        List<FieldError> errors)
    {
        bool isSequential = questions
            .Select(q => q.QuestionNumber)
            .OrderBy(n => n)
            .SequenceEqual(Enumerable.Range(1, questions.Count));

        if (!isSequential)
        {
            errors.Add(new FieldError("questions",
                $"Question numbers must be unique and sequential from 1 to {questions.Count}."));
        }
    }

    private static void ValidateQuestionFields(
        QuestionValidationModel question,
        int index,
        List<FieldError> errors)
    {
        if (string.IsNullOrWhiteSpace(question.Text))
            errors.Add(new FieldError($"questions[{index}].text", "Question text is required."));

        if (string.IsNullOrWhiteSpace(question.Answer))
            errors.Add(new FieldError($"questions[{index}].answer", "Answer is required."));

        if (question.CostPositive is < DefaultSettings.QuestionsCostMin or > DefaultSettings.QuestionsCostMax)
        {
            errors.Add(new FieldError($"questions[{index}].costPositive",
                $"CostPositive must be between {DefaultSettings.QuestionsCostMin} and {DefaultSettings.QuestionsCostMax}."));
        }

        if (question.CostNegative is < DefaultSettings.QuestionsCostMin or > DefaultSettings.QuestionsCostMax)
        {
            errors.Add(new FieldError($"questions[{index}].costNegative",
                $"CostNegative must be between {DefaultSettings.QuestionsCostMin} and {DefaultSettings.QuestionsCostMax}."));
        }
    }
}
