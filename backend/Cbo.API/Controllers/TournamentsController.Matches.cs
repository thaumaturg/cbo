using Cbo.API.Cqrs;
using Cbo.API.ErrorHandling;
using Cbo.API.Models.DTO;
using Cbo.API.Queries.Matches;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

public partial class TournamentsController
{
    [HttpGet]
    [Route("{tournamentId:guid}/matches")]
    [Authorize]
    [ProducesResponseType<List<GetMatchDto>>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAllMatches(
        [FromRoute] Guid tournamentId,
        [FromServices] IQueryHandler<GetMatchesQuery, List<GetMatchDto>> getMatches,
        CancellationToken cancellationToken)
    {
        Result<List<GetMatchDto>> result = await getMatches.HandleAsync(new GetMatchesQuery(tournamentId), cancellationToken);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpGet]
    [Route("{tournamentId:guid}/matches/{matchId:guid}")]
    [Authorize]
    [ProducesResponseType<GetMatchDetailDto>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMatchById(
        [FromRoute] Guid tournamentId,
        [FromRoute] Guid matchId,
        [FromServices] IQueryHandler<GetMatchQuery, GetMatchDetailDto> getMatch,
        CancellationToken cancellationToken)
    {
        Result<GetMatchDetailDto> result = await getMatch.HandleAsync(new GetMatchQuery(tournamentId, matchId), cancellationToken);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpGet]
    [Route("{tournamentId:guid}/matches/{matchId:guid}/available-topics")]
    [Authorize]
    [ProducesResponseType<List<GetAvailableTopicDto>>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailableTopicsForMatch(
        [FromRoute] Guid tournamentId,
        [FromRoute] Guid matchId,
        [FromServices] IQueryHandler<GetAvailableTopicsQuery, List<GetAvailableTopicDto>> getAvailableTopics,
        CancellationToken cancellationToken)
    {
        Result<List<GetAvailableTopicDto>> result = await getAvailableTopics.HandleAsync(
            new GetAvailableTopicsQuery(tournamentId, matchId), cancellationToken);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }
}
