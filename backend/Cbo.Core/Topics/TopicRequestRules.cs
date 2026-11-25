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
    /// The number of questions, their numbering and their total cost.
    /// </summary>
    public static IRuleBuilderOptions<T, ICollection<TQuestion>> QuestionList<T, TQuestion>(
        this IRuleBuilder<T, ICollection<TQuestion>> rule)
        where TQuestion : IQuestionRequest =>
        rule
            .Must(questions => questions is not null && questions.Count >= Limits.Topics.QuestionsMin)
            .WithMessage($"A topic must have at least {Limits.Topics.QuestionsMin} question(s).")
            .Must(questions => questions.Count <= Limits.Topics.QuestionsMax)
            .WithMessage($"A topic cannot have more than {Limits.Topics.QuestionsMax} questions.")
            .Must(AreNumberedFromOne)
            .WithMessage((_, questions) =>
                $"Question numbers must be unique and sequential from 1 to {questions.Count}.")
            .Must(questions => TotalCost(questions) <= Limits.Topics.QuestionsCostTotalMax)
            .WithMessage((_, questions) =>
                $"The total cost of a topic's questions cannot be more than {Limits.Topics.QuestionsCostTotalMax} (currently {TotalCost(questions)}).");

    public static IRuleBuilderOptions<T, ICollection<TopicAuthorRequest>> CoAuthorCount<T>(
        this IRuleBuilder<T, ICollection<TopicAuthorRequest>> rule) =>
        rule
            .Must(authors => authors is null || authors.Count <= Limits.Topics.CoAuthorsMax)
            .WithMessage($"A topic can have at most {Limits.Topics.CoAuthorsMax} co-authors.");

    /// <summary>
    /// Each co-author has a username, listed only once. Usernames compare the way Identity compares them, so "Bob"
    /// and "bob" are the same account. The first listing is fine; every later one is an error.
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
            foreach (TopicAuthorRequest author in authors)
            {
                string field = $"{context.PropertyPath}[{index}].username";

                if (string.IsNullOrEmpty(author.Username))
                    context.AddFailure(new ValidationFailure(field, "Username is required."));
                else if (!seenNormalizedUsernames.Add(lookupNormalizer.NormalizeName(author.Username)))
                    context.AddFailure(new ValidationFailure(field, "This author appears more than once."));

                index++;
            }
        });

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
