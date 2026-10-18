using Cbo.API.ErrorHandling;
using Cbo.API.Models.DTO;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

public partial class TournamentsController
{
    [HttpGet]
    [Route("{tournamentId:guid}/matches/{matchId:guid}/rounds/{roundNumber:int}")]
    [Authorize]
    public async Task<IActionResult> GetRound(
        [FromRoute] Guid tournamentId,
        [FromRoute] Guid matchId,
        [FromRoute] int roundNumber)
    {
        Result<GetRoundDto> result = await _roundService.GetRoundAsync(tournamentId, matchId, roundNumber);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpPost]
    [Route("{tournamentId:guid}/matches/{matchId:guid}/rounds")]
    [Authorize]
    public async Task<IActionResult> CreateRound(
        [FromRoute] Guid tournamentId,
        [FromRoute] Guid matchId,
        [FromBody] CreateRoundWithAnswersDto createRoundDto)
    {
        Result<GetRoundDto> result = await _roundService.CreateRoundAsync(tournamentId, matchId, createRoundDto);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        GetRoundDto roundDto = result.Value;
        return CreatedAtAction(nameof(GetRound), new { tournamentId, matchId, roundNumber = roundDto.NumberInMatch }, roundDto);
    }

    [HttpPut]
    [Route("{tournamentId:guid}/matches/{matchId:guid}/rounds/{roundNumber:int}")]
    [Authorize]
    public async Task<IActionResult> UpdateRound(
        [FromRoute] Guid tournamentId,
        [FromRoute] Guid matchId,
        [FromRoute] int roundNumber,
        [FromBody] CreateRoundWithAnswersDto updateRoundDto)
    {
        Result<GetRoundDto> result = await _roundService.UpdateRoundAsync(tournamentId, matchId, roundNumber, updateRoundDto);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpDelete]
    [Route("{tournamentId:guid}/matches/{matchId:guid}/rounds/{roundNumber:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteRound(
        [FromRoute] Guid tournamentId,
        [FromRoute] Guid matchId,
        [FromRoute] int roundNumber)
    {
        Result result = await _roundService.DeleteRoundAsync(tournamentId, matchId, roundNumber);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return NoContent();
    }
}
