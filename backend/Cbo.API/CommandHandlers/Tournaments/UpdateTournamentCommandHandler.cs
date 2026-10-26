using Cbo.API.Authorization;
using Cbo.API.Commands.Tournaments;
using Cbo.API.Cqrs;
using Cbo.API.Data;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Services;
using Cbo.Results;

namespace Cbo.API.CommandHandlers.Tournaments;

public sealed class UpdateTournamentCommandHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService) : CommandHandler<UpdateTournamentCommand>(dbContext)
{
    private const string UpdateForbiddenMessage = "You don't have permission to edit this tournament.";
    private const string InvalidTournamentMessage = "Some of the tournament data is invalid.";

    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;

    protected override async Task<Result> ExecuteAsync(UpdateTournamentCommand command, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            command.TournamentId, TournamentOperations.Update, UpdateForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        TournamentSettings settings = TournamentSettings.Resolve(
            command.PlayersPerTournament,
            command.TopicsPerParticipantMax,
            command.TopicsPerParticipantMin);

        List<FieldError> fieldErrors = settings.Validate();
        if (fieldErrors.Count > 0)
            return Failure.Invalid(InvalidTournamentMessage, fieldErrors);

        Tournament tournament = access.Value;

        if (tournament.CurrentStage != TournamentStage.Preparations)
            return Failure.Conflict("A tournament can only be edited during the Preparations stage.");

        tournament.Title = command.Title;
        tournament.Description = command.Description;
        tournament.PlayersPerTournament = settings.PlayersPerTournament;
        tournament.TopicsPerParticipantMax = settings.TopicsPerParticipantMax;
        tournament.TopicsPerParticipantMin = settings.TopicsPerParticipantMin;

        return Result.Success();
    }
}
