using System;
using System.Linq;
using Cbo.Core.Rounds;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Rounds;

public class CreateRoundRequestValidatorTests : RoundRequestValidatorTests<CreateRoundRequest>
{
    private readonly CreateRoundRequestValidator _validator = new();

    protected override string InvalidMessage => _validator.InvalidMessage;

    protected override TestValidationResult<CreateRoundRequest> Validate(RoundData round) =>
        _validator.TestValidate(new CreateRoundRequest
        {
            NumberInMatch = round.NumberInMatch,
            TopicId = Guid.NewGuid(),
            IsOverrideMode = round.IsOverrideMode,
            Answers =
            [
                .. round.Answers.Select(a => a is null ? null! : new CreateRoundAnswerRequest
                {
                    QuestionId = a.QuestionId,
                    MatchParticipantId = a.MatchParticipantId,
                    IsAnswerAccepted = a.IsAnswerAccepted,
                    OverrideCost = a.OverrideCost,
                }),
            ],
        });
}
