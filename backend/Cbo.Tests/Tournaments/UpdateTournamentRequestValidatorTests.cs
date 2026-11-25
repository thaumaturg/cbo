using Cbo.Core.Tournaments;
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
            PlayersPerTournament = tournament.PlayersPerTournament,
            TopicsPerParticipantMax = tournament.TopicsPerParticipantMax,
            TopicsPerParticipantMin = tournament.TopicsPerParticipantMin,
        });
}
