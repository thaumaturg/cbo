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

public sealed class GetMatchQueryHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService) : IQueryHandler<GetMatchQuery, GetMatchDetailDto>
{
    private const string ReadForbiddenMessage = "You don't have permission to view this tournament.";
    private const string NotFoundMessage = "Match not found in this tournament.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;

    public async Task<Result<GetMatchDetailDto>> HandleAsync(GetMatchQuery query, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            query.TournamentId, TournamentOperations.Read, ReadForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Expression<Func<MatchParticipant, GetMatchParticipantDto>> toParticipantDto = MatchParticipantProjections.ToGetDto();
        Expression<Func<Round, GetRoundDto>> toRoundDto = RoundProjections.ToGetDto();

        GetMatchDetailDto? match = await _dbContext.Matches
            .Where(m => m.Id == query.MatchId && m.TournamentId == query.TournamentId)
            .Select(m => new GetMatchDetailDto
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
                Rounds = m.Rounds
                    .AsQueryable()
                    .OrderBy(r => r.NumberInMatch)
                    .Select(toRoundDto)
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (match is null)
            return Failure.NotFound(NotFoundMessage);

        return match;
    }
}
