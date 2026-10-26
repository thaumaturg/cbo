using Cbo.API.Authorization;
using Cbo.API.Cqrs;
using Cbo.API.Data;
using Cbo.API.Mappings;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Queries.Tournaments;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.QueryHandlers.Tournaments;

public sealed class GetTournamentQueryHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService,
    ICurrentUserService currentUserService) : IQueryHandler<GetTournamentQuery, GetTournamentDto>
{
    private const string ReadForbiddenMessage = "You don't have permission to view this tournament.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<GetTournamentDto>> HandleAsync(GetTournamentQuery query, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            query.TournamentId, TournamentOperations.Read, ReadForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        return await _dbContext.Tournaments
            .Where(t => t.Id == query.TournamentId)
            .Select(TournamentProjections.ToGetDto(currentUserId))
            .SingleAsync(cancellationToken);
    }
}
