using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Results;

namespace Cbo.Core.Tournaments;

public sealed class UpdateTournamentCommandHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService) : CommandHandler<UpdateTournamentCommand>(dbContext)
{
    private const string UpdateForbiddenMessage = "You don't have permission to edit this tournament.";

    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;

    protected override async Task<Result> ExecuteAsync(UpdateTournamentCommand command, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            command.TournamentId, TournamentOperations.Update, UpdateForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Tournament tournament = access.Value;

        if (tournament.CurrentStage != TournamentStage.Preparations)
            return Failure.Conflict("A tournament can only be edited during the Preparations stage.");

        TournamentSettings settings = TournamentSettings.Resolve(
            command.PlayersPerTournament,
            command.TopicsPerParticipantMax,
            command.TopicsPerParticipantMin);

        tournament.Title = command.Title;
        tournament.Description = command.Description;
        tournament.PlayersPerTournament = settings.PlayersPerTournament;
        tournament.TopicsPerParticipantMax = settings.TopicsPerParticipantMax;
        tournament.TopicsPerParticipantMin = settings.TopicsPerParticipantMin;

        return Result.Success();
    }
}
