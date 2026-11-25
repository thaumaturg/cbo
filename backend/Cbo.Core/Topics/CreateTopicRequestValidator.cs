using Cbo.Core.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace Cbo.Core.Topics;

public sealed class CreateTopicRequestValidator : RequestValidator<CreateTopicRequest>
{
    public CreateTopicRequestValidator(ILookupNormalizer lookupNormalizer)
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
