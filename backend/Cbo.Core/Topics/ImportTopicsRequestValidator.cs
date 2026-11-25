using Cbo.Core.Domain;
using Cbo.Core.Validation;
using FluentValidation;

namespace Cbo.Core.Topics;

/// <summary>
/// Checks the size of an import and then each of its topics. The topics of an import that is empty or too big are
/// not checked.
/// </summary>
public sealed class ImportTopicsRequestValidator : RequestValidator<ImportTopicsRequest>
{
    public ImportTopicsRequestValidator()
    {
        RuleFor(import => import.Topics)
            .Must(topics => topics is not null && topics.Count >= 1)
            .WithMessage("Choose at least 1 topic to import.")
            .Must(topics => topics.Count <= Limits.Topics.ImportMax)
            .WithMessage($"You can import at most {Limits.Topics.ImportMax} topics at once.")
            .DependentRules(() =>
                RuleForEach(import => import.Topics).SetValidator(new ImportTopicRequestValidator()));
    }

    public override string InvalidMessage => TopicFailures.InvalidImportMessage;
}
