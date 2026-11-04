using Cbo.Core.Authorization;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Matches;
using Cbo.Core.Models.Domain;
using Cbo.Core.Tournaments;
using Cbo.Results;

namespace Cbo.Core.Rounds;

/// <summary>
/// Removes a round with its answers, then recalculates the match's and the tournament's scores.
/// </summary>
public sealed class DeleteRoundCommandHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService,
    IMatchScoringService matchScoringService) : CommandHandler<DeleteRoundCommand>(dbContext)
{
    private const string ManageRoundsForbiddenMessage = "You don't have permission to manage rounds in this tournament.";

    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;
    private readonly IMatchScoringService _matchScoringService = matchScoringService;

    protected override async Task<Result> ExecuteAsync(DeleteRoundCommand command, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            command.TournamentId, TournamentOperations.ManageRounds, ManageRoundsForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Match? match = await _matchScoringService.LoadMatchAsync(command.TournamentId, command.MatchId, cancellationToken);
        if (match is null)
            return RoundFailures.MatchNotFound();

        Round? round = match.Rounds.FirstOrDefault(r => r.NumberInMatch == command.RoundNumber);
        if (round is null)
            return RoundFailures.RoundNotFound(command.RoundNumber);

        // The round's answers are tracked, so EF deletes them with it
        match.Rounds.Remove(round);
        DbContext.Rounds.Remove(round);

        await _matchScoringService.RecalculateScoresAsync(match, cancellationToken);

        return Result.Success();
    }
}
