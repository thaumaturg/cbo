using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Models.Domain;
using Cbo.Core.Tournaments;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Rounds;

public sealed class GetRoundQueryHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService) : IQueryHandler<GetRoundQuery, RoundResponse>
{
    private const string ReadForbiddenMessage = "You don't have permission to view this tournament.";
    private const string MatchNotFoundMessage = "Match not found in this tournament.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;

    public async Task<Result<RoundResponse>> HandleAsync(GetRoundQuery query, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            query.TournamentId, TournamentOperations.Read, ReadForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        bool matchExists = await _dbContext.Matches
            .AnyAsync(m => m.Id == query.MatchId && m.TournamentId == query.TournamentId, cancellationToken);
        if (!matchExists)
            return Failure.NotFound(MatchNotFoundMessage);

        RoundResponse? round = await _dbContext.Rounds
            .Where(r => r.MatchId == query.MatchId && r.NumberInMatch == query.RoundNumber)
            .Select(RoundProjections.ToResponse())
            .FirstOrDefaultAsync(cancellationToken);
        if (round is null)
            return Failure.NotFound($"Round {query.RoundNumber} not found for this match.");

        return round;
    }
}
