using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Core.Tournaments;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Playoffs;

/// <summary>
/// Lists the matches reopening a match would delete, with how many rounds each has, in bracket order with each
/// match's shootouts after it. Fails as Reopen would.
/// </summary>
public sealed class GetReopenDeletionsQueryHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService) : IQueryHandler<GetReopenDeletionsQuery, List<ReopenDeletionResponse>>
{
    private const string ForbiddenMessage = "You don't have permission to reopen matches in this tournament.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;

    public async Task<Result<List<ReopenDeletionResponse>>> HandleAsync(GetReopenDeletionsQuery query, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            query.TournamentId, TournamentOperations.ReopenMatches, ForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Match? match = await _dbContext.Matches
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == query.MatchId && m.TournamentId == query.TournamentId, cancellationToken);
        if (match is null)
            return Failure.NotFound("Match not found in this tournament.");

        Failure? failure = PlayoffReopening.Check(access.Value, match);
        if (failure is not null)
            return failure;

        PlayoffReopening reopening = await PlayoffReopening.PlanAsync(_dbContext, access.Value, match, cancellationToken);

        List<ReopenDeletionResponse> deletions = await _dbContext.Matches
            .Where(m => reopening.DeletedMatchIds.Contains(m.Id))
            .Select(m => new ReopenDeletionResponse
            {
                MatchId = m.Id,
                Type = m.Type,
                BracketPosition = m.BracketPosition,
                SettledBracketPosition = m.SettledMatch!.BracketPosition,
                ShootoutNumber = m.ShootoutNumber,
                RoundsCount = m.Rounds.Count,
            })
            .ToListAsync(cancellationToken);

        // Positions are stored as names, so they are put in bracket order here
        return deletions
            .OrderBy(d => d.BracketPosition ?? d.SettledBracketPosition)
            .ThenBy(d => d.ShootoutNumber ?? 0)
            .ToList();
    }
}
