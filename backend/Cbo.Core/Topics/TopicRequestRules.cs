using Cbo.Core.Domain;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;

namespace Cbo.Core.Topics;

/// <summary>
/// Rules for the fields of a topic that the create, update and import request validators share. Each question is
/// checked by <see cref="QuestionRequestValidator"/>.
/// </summary>
internal static class TopicRequestRules
{
    public static IRuleBuilderOptions<T, string> TopicTitle<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("The title is required.")
            .MaximumLength(Limits.Topics.TitleLengthMax)
            .WithMessage($"The title can be at most {Limits.Topics.TitleLengthMax} characters.");

    public static IRuleBuilderOptions<T, string?> TopicDescription<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .MaximumLength(Limits.Topics.DescriptionLengthMax)
            .WithMessage($"The description can be at most {Limits.Topics.DescriptionLengthMax} characters.");

    /// <summary>
    /// The number of questions, their numbering and their total cost. The numbering and the cost wait until no question
    /// is missing; <see cref="EachQuestion"/> reports a missing one.
    /// </summary>
    public static IRuleBuilderOptions<T, ICollection<TQuestion>> QuestionList<T, TQuestion>(
        this IRuleBuilder<T, ICollection<TQuestion>> rule)
        where TQuestion : IQuestionRequest =>
        rule
            .Must(questions => questions is not null && questions.Count >= Limits.Topics.QuestionsMin)
            .WithMessage($"A topic must have at least {Limits.Topics.QuestionsMin} question(s).")
            .Must(questions => questions.Count <= Limits.Topics.QuestionsMax)
            .WithMessage($"A topic cannot have more than {Limits.Topics.QuestionsMax} questions.")
            .Must(questions => HasMissingQuestion(questions) || AreNumberedFromOne(questions))
            .WithMessage((_, questions) =>
                $"Question numbers must be unique and sequential from 1 to {questions.Count}.")
            .Must(questions => HasMissingQuestion(questions) || TotalCost(questions) <= Limits.Topics.QuestionsCostTotalMax)
            .WithMessage((_, questions) =>
                $"The total cost of a topic's questions cannot be more than {Limits.Topics.QuestionsCostTotalMax} (currently {TotalCost(questions)}).");

    /// <summary>
    /// Checks every question with <see cref="QuestionRequestValidator"/>. A null in the list is a missing question.
    /// </summary>
    public static IRuleBuilderOptions<T, TQuestion> EachQuestion<T, TQuestion>(this IRuleBuilderInitialCollection<T, TQuestion> rule)
        where TQuestion : class, IQuestionRequest =>
        // A child validator skips a null item, so the null check is the only error for one
        rule
            .NotNull().WithMessage("This question is missing.")
            .SetValidator(new QuestionRequestValidator());

    public static IRuleBuilderOptions<T, ICollection<TopicAuthorRequest>> CoAuthorCount<T>(
        this IRuleBuilder<T, ICollection<TopicAuthorRequest>> rule) =>
        rule
            .Must(authors => authors is null || authors.Count <= Limits.Topics.CoAuthorsMax)
            .WithMessage($"A topic can have at most {Limits.Topics.CoAuthorsMax} co-authors.");

    /// <summary>
    /// Each co-author is present, has a username, and is listed only once. Usernames compare the way Identity compares
    /// them, so "Bob" and "bob" are the same account. The first listing is fine; every later one is an error.
    /// </summary>
    public static void CoAuthorUsernames<T>(
        this IRuleBuilder<T, ICollection<TopicAuthorRequest>> rule, ILookupNormalizer lookupNormalizer) =>
        // One pass over the whole list, since a repeat depends on the authors before it
        rule.Custom((authors, context) =>
        {
            if (authors is null)
                return;

            HashSet<string> seenNormalizedUsernames = [];
            int index = 0;
            foreach (TopicAuthorRequest? author in authors)
            {
                string field = $"{context.PropertyPath}[{index}].username";

                if (author is null)
                    context.AddFailure(new ValidationFailure($"{context.PropertyPath}[{index}]", "This co-author is missing."));
                else if (string.IsNullOrEmpty(author.Username))
                    context.AddFailure(new ValidationFailure(field, "Username is required."));
                else if (!seenNormalizedUsernames.Add(lookupNormalizer.NormalizeName(author.Username)))
                    context.AddFailure(new ValidationFailure(field, "This author appears more than once."));

                index++;
            }
        });

    private static bool HasMissingQuestion<TQuestion>(ICollection<TQuestion> questions)
        where TQuestion : IQuestionRequest =>
        questions.Any(q => q is null);

    private static bool AreNumberedFromOne<TQuestion>(ICollection<TQuestion> questions)
        where TQuestion : IQuestionRequest =>
        questions
            .Select(q => q.QuestionNumber)
            .Order()
            .SequenceEqual(Enumerable.Range(1, questions.Count));

    private static long TotalCost<TQuestion>(ICollection<TQuestion> questions)
        where TQuestion : IQuestionRequest =>
        questions.Sum(q => (long)q.Cost);
}
