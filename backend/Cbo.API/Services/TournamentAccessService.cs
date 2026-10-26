using Cbo.API.Data;
using Cbo.API.Models.Domain;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Services;

public interface ITournamentAccessService
{
    /// <summary>
    /// Loads the tournament from the URL, tracked so command handlers can change it, and checks the current user's permission on it.
    /// </summary>
    Task<Result<Tournament>> LoadTournamentWithAccessAsync(
        Guid tournamentId,
        OperationAuthorizationRequirement operation,
        string forbiddenMessage,
        CancellationToken cancellationToken = default);
}

public class TournamentAccessService(
    CboDbContext dbContext,
    IAuthorizationService authorizationService,
    ICurrentUserService currentUserService) : ITournamentAccessService
{
    private readonly CboDbContext _dbContext = dbContext;
    private readonly IAuthorizationService _authorizationService = authorizationService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<Tournament>> LoadTournamentWithAccessAsync(
        Guid tournamentId,
        OperationAuthorizationRequirement operation,
        string forbiddenMessage,
        CancellationToken cancellationToken = default)
    {
        Tournament? tournament = await _dbContext.Tournaments
            .FirstOrDefaultAsync(t => t.Id == tournamentId, cancellationToken);
        if (tournament is null)
            return Failure.NotFound("Tournament not found.");

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(_currentUserService.GetRequiredCurrentUser(), tournament, operation);
        if (!authResult.Succeeded)
            return Failure.Forbidden(forbiddenMessage);

        return tournament;
    }
}
