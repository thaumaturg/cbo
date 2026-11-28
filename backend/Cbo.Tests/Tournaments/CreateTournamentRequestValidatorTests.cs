using Cbo.Core.Tournaments;
using Cbo.Tests.Validation;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Tournaments;

public class CreateTournamentRequestValidatorTests : TournamentRequestValidatorTests<CreateTournamentRequest>
{
    private readonly CreateTournamentRequestValidator _validator = new();

    protected override string InvalidMessage => _validator.InvalidMessage;

    protected override TestValidationResult<CreateTournamentRequest> Validate(TournamentData tournament) =>
        _validator.TestValidate(new CreateTournamentRequest
        {
            Title = tournament.Title,
            Description = tournament.Description,
            Format = tournament.Format,
            TopicsPerParticipantMax = tournament.TopicsPerParticipantMax,
            TopicsPerParticipantMin = tournament.TopicsPerParticipantMin,
        });

    protected override TestValidationResult<CreateTournamentRequest> ValidateJson(string json) =>
        _validator.TestValidate(ApiJson.Read<CreateTournamentRequest>(json));
}
