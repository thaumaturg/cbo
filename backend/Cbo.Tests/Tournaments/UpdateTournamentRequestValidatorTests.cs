using Cbo.Core.Tournaments;
using Cbo.Tests.Validation;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Tournaments;

public class UpdateTournamentRequestValidatorTests : TournamentRequestValidatorTests<UpdateTournamentRequest>
{
    private readonly UpdateTournamentRequestValidator _validator = new();

    protected override string InvalidMessage => _validator.InvalidMessage;

    protected override TestValidationResult<UpdateTournamentRequest> Validate(TournamentData tournament) =>
        _validator.TestValidate(new UpdateTournamentRequest
        {
            Title = tournament.Title,
            Description = tournament.Description,
            Format = tournament.Format,
            TopicsPerParticipantMax = tournament.TopicsPerParticipantMax,
            TopicsPerParticipantMin = tournament.TopicsPerParticipantMin,
        });

    protected override TestValidationResult<UpdateTournamentRequest> ValidateJson(string json) =>
        _validator.TestValidate(ApiJson.Read<UpdateTournamentRequest>(json));
}
