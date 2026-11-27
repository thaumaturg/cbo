using System.Linq.Expressions;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Tournaments;

/// <summary>
/// Rolls a tournament back to an earlier stage: keeps that stage's data and wipes everything played after it.
/// Topics played in the wiped rounds count as unplayed again.
/// </summary>
public sealed class RollBackCommandHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService) : CommandHandler<RollBackCommand>(dbContext)
{
    private const string ForbiddenMessage = "You don't have permission to roll this tournament back.";

    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;

    protected override async Task<Result> ExecuteAsync(RollBackCommand command, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LockTournamentWithAccessAsync(
            command.TournamentId, TournamentOperations.ChangeStage, ForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Tournament tournament = access.Value;

        if (command.Stage >= tournament.CurrentStage)
        {
            return Failure.Conflict(
                $"A tournament can only be rolled back to an earlier stage. This one is in {tournament.CurrentStage}.");
        }

        // Rolling back past a stage wipes what was played in it, the latest stage first. See A10 in docs/specs/playoff.md
        tournament.EndedAt = null;
        if (command.Stage < TournamentStage.Playoff)
            await WipePlayoffAsync(tournament, cancellationToken);
        if (command.Stage < TournamentStage.Qualifications)
            await WipeQualificationsAsync(tournament, cancellationToken);

        tournament.CurrentStage = command.Stage;

        return Result.Success();
    }

    /// <summary>Deletes the playoff matches, shootouts and the Final, and unfixes the qualification places.</summary>
    private async Task WipePlayoffAsync(Tournament tournament, CancellationToken cancellationToken)
    {
        await DeleteMatchesAsync(m => m.TournamentId == tournament.Id && m.Type != MatchType.Qualification, cancellationToken);

        foreach (TournamentParticipant player in await PlayersAsync(tournament, cancellationToken))
            player.QualificationPlace = null;
    }

    /// <summary>Deletes the qualification matches, and the totals and start time they gave.</summary>
    private async Task WipeQualificationsAsync(Tournament tournament, CancellationToken cancellationToken)
    {
        await DeleteMatchesAsync(m => m.TournamentId == tournament.Id, cancellationToken);

        foreach (TournamentParticipant player in await PlayersAsync(tournament, cancellationToken))
        {
            player.PointsSum = null;
            player.ScoreSum = null;
            player.PositiveScoreSum = null;
        }

        tournament.StartedAt = null;
    }

    /// <summary>Deletes the matches with their rounds, answers and players.</summary>
    private async Task DeleteMatchesAsync(Expression<Func<Match, bool>> matchFilter, CancellationToken cancellationToken)
    {
        // Answers block the cascade from their match's players, so they go first
        IQueryable<Match> matches = DbContext.Matches.Where(matchFilter);
        await DbContext.RoundAnswers
            .Where(ra => matches.Contains(ra.Round!.Match!))
            .ExecuteDeleteAsync(cancellationToken);
        await matches.ExecuteDeleteAsync(cancellationToken);
    }

    private Task<List<TournamentParticipant>> PlayersAsync(Tournament tournament, CancellationToken cancellationToken) =>
        DbContext.TournamentParticipants
            .Where(tp => tp.TournamentId == tournament.Id && tp.Role == TournamentParticipantRole.Player)
            .ToListAsync(cancellationToken);
}
