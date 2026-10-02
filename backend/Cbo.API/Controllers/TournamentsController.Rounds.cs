using Cbo.API.Extensions;
using Cbo.API.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

public partial class TournamentsController
{
    private const string ProblemJson = "application/problem+json";

    [HttpPost]
    [Route("{tournamentId:guid}/matches/{matchId:guid}/rounds")]
    [Authorize]
    [ProducesResponseType<GetRoundDto>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status409Conflict, ProblemJson)]
    public async Task<IActionResult> CreateRound(
        [FromRoute] Guid tournamentId,
        [FromRoute] Guid matchId,
        [FromBody] CreateRoundWithAnswersDto createRoundDto,
        CancellationToken cancellationToken)
    {
        return (await _roundService.CreateRoundAsync(tournamentId, matchId, createRoundDto, User, cancellationToken)).ToActionResult(this);
    }

    [HttpPut]
    [Route("{tournamentId:guid}/matches/{matchId:guid}/rounds/{roundNumber:int}")]
    [Authorize]
    [ProducesResponseType<GetRoundDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemJson)]
    public async Task<IActionResult> UpdateRound(
        [FromRoute] Guid tournamentId,
        [FromRoute] Guid matchId,
        [FromRoute] int roundNumber,
        [FromBody] CreateRoundWithAnswersDto updateRoundDto,
        CancellationToken cancellationToken)
    {
        return (await _roundService.UpdateRoundAsync(tournamentId, matchId, roundNumber, updateRoundDto, User, cancellationToken)).ToActionResult(this);
    }

    [HttpDelete]
    [Route("{tournamentId:guid}/matches/{matchId:guid}/rounds/{roundNumber:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    public async Task<IActionResult> DeleteRound(
        [FromRoute] Guid tournamentId,
        [FromRoute] Guid matchId,
        [FromRoute] int roundNumber,
        CancellationToken cancellationToken)
    {
        return (await _roundService.DeleteRoundAsync(tournamentId, matchId, roundNumber, User, cancellationToken)).ToActionResult(this);
    }
}
