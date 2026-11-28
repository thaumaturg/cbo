using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Data.Configuration;
using Cbo.Core.Domain;
using Cbo.Core.Matches;
using Cbo.Core.Participants;
using Cbo.Core.Playoffs;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Tournaments;

/// <summary>
/// Moves a tournament from Qualifications to the Playoff: fixes the qualification places and creates the Playoff's
/// first matches. Qualification matches are locked from then on.
/// </summary>
public sealed class StartPlayoffCommandHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService,
    IPlayoffProgressionService playoffProgressionService) : CommandHandler<StartPlayoffCommand>(dbContext)
{
    private const string ForbiddenMessage = "You don't have permission to start the Playoff of this tournament.";
    private const string NotInQualificationsMessage = "Only a tournament in the Qualifications stage can start its Playoff.";

    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;
    private readonly IPlayoffProgressionService _playoffProgressionService = playoffProgressionService;

    protected override async Task<Result> ExecuteAsync(StartPlayoffCommand command, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LockTournamentWithAccessAsync(
            command.TournamentId, TournamentOperations.ChangeStage, ForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Tournament tournament = access.Value;

        if (tournament.CurrentStage != TournamentStage.Qualifications)
            return Failure.Conflict(NotInQualificationsMessage);

        if (PlayoffBracket.For(tournament.Format) is null)
            return Failure.Conflict("This tournament's format has no playoff bracket yet, so its Playoff can't start.");

        List<int> roundsPlayed = await DbContext.Matches
            .Where(m => m.TournamentId == tournament.Id && m.Type == MatchType.Qualification)
            .Select(m => m.Rounds.Count)
            .ToListAsync(cancellationToken);
        int unfinished = roundsPlayed.Count(rounds => !MatchType.Qualification.IsFinished(rounds));
        if (unfinished > 0)
        {
            string notFinished = unfinished == 1 ? "1 qualification match isn't" : $"{unfinished} qualification matches aren't";
            return Failure.Conflict(
                $"Every qualification match must be finished before the Playoff starts. {notFinished} finished yet.");
        }

        List<TournamentParticipant> players = await DbContext.TournamentParticipants
            .Where(tp => tp.TournamentId == tournament.Id && tp.Role == TournamentParticipantRole.Player)
            .ToListAsync(cancellationToken);

        int place = 1;
        foreach (TournamentParticipant player in QualificationPlaces.InPlaceOrder(players, ResultOf))
            player.QualificationPlace = place++;

        tournament.CurrentStage = TournamentStage.Playoff;

        await _playoffProgressionService.CreateNextMatchesAsync(tournament, cancellationToken);

        return Result.Success();
    }

    protected override Failure? MapSaveFailure(StartPlayoffCommand command, DbUpdateException exception)
    {
        // Lost a race with a simultaneous start of the same Playoff
        if (PostgresConstraint.IsUniqueViolation(exception, MatchConfiguration.TournamentIdNumberInTournamentIndexName))
            return Failure.Conflict(NotInQualificationsMessage);

        return null;
    }

    private static QualificationResult ResultOf(TournamentParticipant player) =>
        new(
            player.PointsSum ?? 0m,
            player.ScoreSum ?? 0,
            player.PositiveScoreSum ?? 0,
            player.Seed ?? throw new InvalidOperationException($"Player {player.Id} has no seed."));
}
