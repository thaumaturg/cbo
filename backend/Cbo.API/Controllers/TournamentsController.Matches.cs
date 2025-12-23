using Cbo.API.Authorization;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

public partial class TournamentsController
{
    [HttpGet]
    [Route("{tournamentId:int}/matches")]
    [Authorize]
    public async Task<IActionResult> GetAllMatches([FromRoute] int tournamentId)
    {
        Tournament? tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament is null)
            return NotFound();

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(User, tournament, TournamentOperations.Read);
        if (!authResult.Succeeded)
            return NotFound();

        List<Match> matchesDomain = await _matchRepository.GetAllByTournamentIdAsync(tournamentId);
        List<GetMatchDto> matchesDto = _mapper.Map<List<GetMatchDto>>(matchesDomain);

        return Ok(matchesDto);
    }
}
