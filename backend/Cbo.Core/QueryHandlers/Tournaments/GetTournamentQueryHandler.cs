using Cbo.Core.Authorization;
using Cbo.Core.Contracts.Tournaments;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Mappings;
using Cbo.Core.Models.Domain;
using Cbo.Core.Queries.Tournaments;
using Cbo.Core.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.QueryHandlers.Tournaments;

public sealed class GetTournamentQueryHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService,
    ICurrentUserService currentUserService) : IQueryHandler<GetTournamentQuery, TournamentResponse>
{
    private const string ReadForbiddenMessage = "You don't have permission to view this tournament.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<TournamentResponse>> HandleAsync(GetTournamentQuery query, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            query.TournamentId, TournamentOperations.Read, ReadForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        return await _dbContext.Tournaments
            .Where(t => t.Id == query.TournamentId)
            .Select(TournamentProjections.ToResponse(currentUserId))
            .SingleAsync(cancellationToken);
    }
}
