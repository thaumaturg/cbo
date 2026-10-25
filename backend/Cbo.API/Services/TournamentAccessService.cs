using Cbo.API.Models.Domain;
using Cbo.API.Repositories;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Cbo.API.Services;

public interface ITournamentAccessService
{
    /// <summary>
    /// Loads the tournament from the URL and checks the current user's permission on it.
    /// </summary>
    Task<Result<Tournament>> LoadTournamentWithAccessAsync(
        Guid tournamentId,
        OperationAuthorizationRequirement operation,
        string forbiddenMessage);
}

public class TournamentAccessService(
    ITournamentRepository tournamentRepository,
    IAuthorizationService authorizationService,
    ICurrentUserService currentUserService) : ITournamentAccessService
{
    private readonly ITournamentRepository _tournamentRepository = tournamentRepository;
    private readonly IAuthorizationService _authorizationService = authorizationService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<Tournament>> LoadTournamentWithAccessAsync(
        Guid tournamentId,
        OperationAuthorizationRequirement operation,
        string forbiddenMessage)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return Failure.NotFound("Tournament not found.");

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(_currentUserService.GetRequiredCurrentUser(), tournament, operation);
        if (!authResult.Succeeded)
            return Failure.Forbidden(forbiddenMessage);

        return tournament;
    }
}
