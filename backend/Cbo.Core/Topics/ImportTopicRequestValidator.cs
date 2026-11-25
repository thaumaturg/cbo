using Cbo.Core.Validation;
using FluentValidation;

namespace Cbo.Core.Topics;

/// <summary>
/// Checks one topic of an import with the same rules as creating a topic. Imported topics have no co-authors.
/// </summary>
public sealed class ImportTopicRequestValidator : ChildValidator<ImportTopicRequest>
{
    public ImportTopicRequestValidator()
    {
        RuleFor(t => t.Title).TopicTitle();
        RuleFor(t => t.Description).TopicDescription();
        RuleFor(t => t.Questions).QuestionList();
        RuleForEach(t => t.Questions).SetValidator(new QuestionRequestValidator());
    }
}
