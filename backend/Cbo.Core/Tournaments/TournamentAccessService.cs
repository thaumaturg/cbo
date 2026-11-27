using Cbo.Core.Accounts;
using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Tournaments;

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

    /// <summary>
    /// Like <see cref="LoadTournamentWithAccessAsync"/>, but also locks the tournament's row until the command's
    /// transaction ends. Commands that change the Playoff, or the rounds it is settled from, take this lock, so they
    /// run one at a time per tournament and each sees what the one before it saved.
    /// </summary>
    Task<Result<Tournament>> LockTournamentWithAccessAsync(
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

        return await AuthorizeAsync(tournament, operation, forbiddenMessage);
    }

    public async Task<Result<Tournament>> LockTournamentWithAccessAsync(
        Guid tournamentId,
        OperationAuthorizationRequirement operation,
        string forbiddenMessage,
        CancellationToken cancellationToken = default)
    {
        // Not composed further, so the locking clause stays at the top level of the query
        List<Tournament> tournaments = await _dbContext.Tournaments
            .FromSql($"SELECT * FROM tournaments WHERE id = {tournamentId} FOR UPDATE")
            .ToListAsync(cancellationToken);

        return await AuthorizeAsync(tournaments.SingleOrDefault(), operation, forbiddenMessage);
    }

    private async Task<Result<Tournament>> AuthorizeAsync(
        Tournament? tournament,
        OperationAuthorizationRequirement operation,
        string forbiddenMessage)
    {
        if (tournament is null)
            return Failure.NotFound("Tournament not found.");

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(_currentUserService.GetRequiredCurrentUser(), tournament, operation);
        if (!authResult.Succeeded)
            return Failure.Forbidden(forbiddenMessage);

        return tournament;
    }
}
