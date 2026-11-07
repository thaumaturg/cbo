using Cbo.Core.Domain;
using Cbo.Results;

namespace Cbo.Core.Topics;

/// <summary>
/// Question fields shared by create and update requests, in the shape the validator needs.
/// </summary>
public record QuestionValidationModel(
    int QuestionNumber,
    int Cost,
    string? Text,
    string? Answer);

/// <summary>
/// A co-author row of a create or update request, with the account its username resolved to.
/// <paramref name="NormalizedUsername"/> is null when the username is blank, and <paramref name="UserId"/> when no
/// account has it.
/// </summary>
public record CoAuthorValidationModel(
    string? Username,
    string? NormalizedUsername,
    Guid? UserId);

public interface ITopicValidationService
{
    /// <summary>
    /// Validates a topic's question collection against the constraints in <see cref="DefaultSettings"/>.
    /// Returns field errors keyed by camelCase JSON path (for example <c>questions[0].text</c>); empty when valid.
    /// </summary>
    List<FieldError> ValidateQuestions(IReadOnlyList<QuestionValidationModel> questions);

    /// <summary>
    /// Validates a topic's co-authors: each one names an existing account other than <paramref name="ownerId"/>, at
    /// most once. Returns field errors keyed by <c>authors[i].username</c>; empty when valid.
    /// </summary>
    List<FieldError> ValidateCoAuthors(IReadOnlyList<CoAuthorValidationModel> coAuthors, Guid ownerId);
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

    public List<FieldError> ValidateCoAuthors(IReadOnlyList<CoAuthorValidationModel> coAuthors, Guid ownerId)
    {
        List<FieldError> errors = [];
        HashSet<string> seenNormalizedUsernames = [];

        for (int i = 0; i < coAuthors.Count; i++)
        {
            CoAuthorValidationModel coAuthor = coAuthors[i];
            string field = $"authors[{i}].username";

            if (coAuthor.NormalizedUsername is null)
                errors.Add(new FieldError(field, "Username is required."));
            else if (!seenNormalizedUsernames.Add(coAuthor.NormalizedUsername))
                errors.Add(new FieldError(field, "This author appears more than once."));
            else if (coAuthor.UserId is null)
                errors.Add(new FieldError(field, $"User '{coAuthor.Username}' doesn't exist."));
            else if (coAuthor.UserId == ownerId)
                errors.Add(new FieldError(field, "The owner can't be added as a co-author."));
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

        if (question.Cost is < DefaultSettings.QuestionsCostMin or > DefaultSettings.QuestionsCostMax)
        {
            errors.Add(new FieldError($"questions[{index}].cost",
                $"Cost must be between {DefaultSettings.QuestionsCostMin} and {DefaultSettings.QuestionsCostMax}."));
        }
    }
}
