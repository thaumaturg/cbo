using System.Security.Claims;
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
    IHttpContextAccessor httpContextAccessor) : ITournamentAccessService
{
    private readonly ITournamentRepository _tournamentRepository = tournamentRepository;
    private readonly IAuthorizationService _authorizationService = authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task<Result<Tournament>> LoadTournamentWithAccessAsync(
        Guid tournamentId,
        OperationAuthorizationRequirement operation,
        string forbiddenMessage)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return Failure.NotFound("Tournament not found.");

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(GetCurrentUser(), tournament, operation);
        if (!authResult.Succeeded)
            return Failure.Forbidden(forbiddenMessage);

        return tournament;
    }

    private ClaimsPrincipal GetCurrentUser()
    {
        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            throw new InvalidOperationException("No authenticated user in the current request. Tournament operations must only be called from [Authorize] endpoints.");

        return user;
    }
}
