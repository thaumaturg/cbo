using Cbo.API.ErrorHandling;
using Cbo.API.Models.DTO;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

public partial class TournamentsController
{
    [HttpGet]
    [Route("{tournamentId:guid}/matches")]
    [Authorize]
    public async Task<IActionResult> GetAllMatches([FromRoute] Guid tournamentId)
    {
        Result<List<GetMatchDto>> result = await _matchService.GetMatchesAsync(tournamentId);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpGet]
    [Route("{tournamentId:guid}/matches/{matchId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetMatchById([FromRoute] Guid tournamentId, [FromRoute] Guid matchId)
    {
        Result<GetMatchDetailDto> result = await _matchService.GetMatchAsync(tournamentId, matchId);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpGet]
    [Route("{tournamentId:guid}/matches/{matchId:guid}/available-topics")]
    [Authorize]
    public async Task<IActionResult> GetAvailableTopicsForMatch([FromRoute] Guid tournamentId, [FromRoute] Guid matchId)
    {
        Result<List<GetAvailableTopicDto>> result = await _matchService.GetAvailableTopicsAsync(tournamentId, matchId);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }
}
