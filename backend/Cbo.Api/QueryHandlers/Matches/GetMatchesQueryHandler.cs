using System.Linq.Expressions;
using Cbo.Api.Authorization;
using Cbo.Api.Contracts.Matches;
using Cbo.Api.Cqrs;
using Cbo.Api.Data;
using Cbo.Api.Mappings;
using Cbo.Api.Models.Domain;
using Cbo.Api.Queries.Matches;
using Cbo.Api.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Api.QueryHandlers.Matches;

public sealed class GetMatchesQueryHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService) : IQueryHandler<GetMatchesQuery, List<MatchResponse>>
{
    private const string ReadForbiddenMessage = "You don't have permission to view this tournament.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;

    public async Task<Result<List<MatchResponse>>> HandleAsync(GetMatchesQuery query, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            query.TournamentId, TournamentOperations.Read, ReadForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Expression<Func<MatchParticipant, MatchParticipantResponse>> toParticipantResponse = MatchParticipantProjections.ToResponse();

        return await _dbContext.Matches
            .Where(m => m.TournamentId == query.TournamentId)
            .OrderBy(m => m.NumberInTournament)
            .Select(m => new MatchResponse
            {
                Id = m.Id,
                NumberInTournament = m.NumberInTournament,
                NumberInStage = m.NumberInStage,
                CreatedOnStage = m.CreatedOnStage,
                Type = m.Type,
                TournamentId = m.TournamentId,
                MatchParticipants = m.MatchParticipants
                    .AsQueryable()
                    .OrderBy(mp => mp.Id)
                    .Select(toParticipantResponse)
                    .ToList(),
                RoundsCount = m.Rounds.Count,
            })
            .ToListAsync(cancellationToken);
    }
}
