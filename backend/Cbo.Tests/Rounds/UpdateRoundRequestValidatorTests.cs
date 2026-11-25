using System;
using System.Linq;
using Cbo.Core.Rounds;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Rounds;

public class UpdateRoundRequestValidatorTests : RoundRequestValidatorTests<UpdateRoundRequest>
{
    private readonly UpdateRoundRequestValidator _validator = new();

    protected override string InvalidMessage => _validator.InvalidMessage;

    protected override TestValidationResult<UpdateRoundRequest> Validate(RoundData round) =>
        _validator.TestValidate(new UpdateRoundRequest
        {
            NumberInMatch = round.NumberInMatch,
            TopicId = Guid.NewGuid(),
            IsOverrideMode = round.IsOverrideMode,
            Answers =
            [
                .. round.Answers.Select(a => new UpdateRoundAnswerRequest
                {
                    QuestionId = a.QuestionId,
                    MatchParticipantId = a.MatchParticipantId,
                    IsAnswerAccepted = a.IsAnswerAccepted,
                    OverrideCost = a.OverrideCost,
                }),
            ],
        });
}
