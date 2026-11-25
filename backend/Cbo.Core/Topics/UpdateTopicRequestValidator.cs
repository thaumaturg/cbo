using Cbo.Core.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace Cbo.Core.Topics;

/// <summary>
/// Whether each question id belongs to the topic needs the database, so <see cref="UpdateTopicCommandHandler"/>
/// checks it.
/// </summary>
public sealed class UpdateTopicRequestValidator : RequestValidator<UpdateTopicRequest>
{
    public UpdateTopicRequestValidator(ILookupNormalizer lookupNormalizer)
    {
        RuleFor(t => t.Title).TopicTitle();
        RuleFor(t => t.Description).TopicDescription();
        RuleFor(t => t.Questions).QuestionList();
        RuleForEach(t => t.Questions).SetValidator(new QuestionRequestValidator());
        RuleFor(t => t.Authors).CoAuthorCount();
        RuleFor(t => t.Authors).CoAuthorUsernames(lookupNormalizer);
    }

    public override string InvalidMessage => TopicFailures.InvalidMessage;
}
