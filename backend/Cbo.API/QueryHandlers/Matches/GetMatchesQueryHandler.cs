using System.Linq.Expressions;
using Cbo.API.Authorization;
using Cbo.API.Cqrs;
using Cbo.API.Data;
using Cbo.API.Mappings;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Queries.Matches;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.QueryHandlers.Matches;

public sealed class GetMatchesQueryHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService) : IQueryHandler<GetMatchesQuery, List<GetMatchDto>>
{
    private const string ReadForbiddenMessage = "You don't have permission to view this tournament.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;

    public async Task<Result<List<GetMatchDto>>> HandleAsync(GetMatchesQuery query, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            query.TournamentId, TournamentOperations.Read, ReadForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Expression<Func<MatchParticipant, GetMatchParticipantDto>> toParticipantDto = MatchParticipantProjections.ToGetDto();

        return await _dbContext.Matches
            .Where(m => m.TournamentId == query.TournamentId)
            .OrderBy(m => m.NumberInTournament)
            .Select(m => new GetMatchDto
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
                    .Select(toParticipantDto)
                    .ToList(),
                RoundsCount = m.Rounds.Count,
            })
            .ToListAsync(cancellationToken);
    }
}
