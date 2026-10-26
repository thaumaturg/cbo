using Cbo.API.Authorization;
using Cbo.API.Cqrs;
using Cbo.API.Data;
using Cbo.API.Mappings;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Queries.Rounds;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.QueryHandlers.Rounds;

public sealed class GetRoundQueryHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService) : IQueryHandler<GetRoundQuery, GetRoundDto>
{
    private const string ReadForbiddenMessage = "You don't have permission to view this tournament.";
    private const string MatchNotFoundMessage = "Match not found in this tournament.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;

    public async Task<Result<GetRoundDto>> HandleAsync(GetRoundQuery query, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            query.TournamentId, TournamentOperations.Read, ReadForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        bool matchExists = await _dbContext.Matches
            .AnyAsync(m => m.Id == query.MatchId && m.TournamentId == query.TournamentId, cancellationToken);
        if (!matchExists)
            return Failure.NotFound(MatchNotFoundMessage);

        GetRoundDto? round = await _dbContext.Rounds
            .Where(r => r.MatchId == query.MatchId && r.NumberInMatch == query.RoundNumber)
            .Select(RoundProjections.ToGetDto())
            .FirstOrDefaultAsync(cancellationToken);
        if (round is null)
            return Failure.NotFound($"Round {query.RoundNumber} not found for this match.");

        return round;
    }
}
