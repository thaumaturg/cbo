using FluentValidation;
using FluentValidation.Results;

namespace Cbo.Core.Rounds;

/// <summary>
/// Rules for the fields that the create and update round request validators share.
/// </summary>
internal static class RoundRequestRules
{
    /// <summary>
    /// The round number is at least 1. Where it ends depends on the match's kind, which
    /// <see cref="RoundRules.CheckRoundNumber"/> checks once the match is loaded.
    /// </summary>
    public static IRuleBuilderOptions<T, int> RoundNumber<T>(this IRuleBuilder<T, int> rule) =>
        rule
            .GreaterThanOrEqualTo(1)
            .WithMessage("Round number must be at least 1.");

    /// <summary>
    /// Checks every answer with the rules of the round's scoring mode. A null in the list is a missing answer.
    /// </summary>
    public static IRuleBuilderOptions<T, TAnswer> RoundAnswer<T, TAnswer>(
        this IRuleBuilderInitialCollection<T, TAnswer> rule, Func<T, bool> isOverrideMode)
        where TAnswer : class, IRoundAnswerRequest
    {
        var standardAnswer = new RoundAnswerRequestValidator(isOverrideMode: false);
        var overrideAnswer = new RoundAnswerRequestValidator(isOverrideMode: true);

        // A child validator skips a null item, so the null check is the only error for one
        return rule
            .NotNull().WithMessage("This answer is missing.")
            .SetValidator((T request) => isOverrideMode(request) ? overrideAnswer : standardAnswer);
    }

    /// <summary>
    /// A participant answers each question at most once. The first answer is fine; every later one is an error.
    /// </summary>
    public static IRuleBuilderOptionsConditions<T, List<TAnswer>> OneAnswerPerParticipantAndQuestion<T, TAnswer>(
        this IRuleBuilder<T, List<TAnswer>> rule)
        where TAnswer : IRoundAnswerRequest =>
        // One pass over the whole list, since a repeat depends on the answers before it
        rule.Custom((answers, context) =>
        {
            if (answers is null)
                return;

            HashSet<(Guid QuestionId, Guid MatchParticipantId)> seenAnswers = [];
            int index = 0;
            foreach (TAnswer answer in answers)
            {
                if (answer is not null && !seenAnswers.Add((answer.QuestionId, answer.MatchParticipantId)))
                {
                    context.AddFailure(new ValidationFailure(
                        $"{context.PropertyPath}[{index}]",
                        $"Participant {answer.MatchParticipantId} has more than one answer to question {answer.QuestionId}."));
                }

                index++;
            }
        });

    /// <summary>
    /// In standard mode, at most one participant answers each question correctly. Each such question is one error.
    /// </summary>
    public static IRuleBuilderOptionsConditions<T, List<TAnswer>> OneCorrectAnswerPerQuestion<T, TAnswer>(
        this IRuleBuilder<T, List<TAnswer>> rule, Func<T, bool> isOverrideMode)
        where TAnswer : IRoundAnswerRequest =>
        rule.Custom((answers, context) =>
        {
            if (answers is null || isOverrideMode(context.InstanceToValidate))
                return;

            IEnumerable<Guid> questionsWithSeveralCorrectAnswers = answers
                .Where(a => a is not null && a.IsAnswerAccepted == true)
                .GroupBy(a => a.QuestionId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (Guid questionId in questionsWithSeveralCorrectAnswers)
            {
                context.AddFailure(new ValidationFailure(
                    context.PropertyPath,
                    $"Question {questionId} has multiple correct answers. Only one correct answer is allowed per question."));
            }
        });
}
