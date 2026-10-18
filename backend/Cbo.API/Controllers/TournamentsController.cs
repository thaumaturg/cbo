using Cbo.API.ErrorHandling;
using Cbo.API.Models.DTO;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public partial class TournamentsController(
    ITournamentService tournamentService,
    IMatchService matchService,
    IRoundService roundService,
    ITournamentParticipantService participantService,
    ITournamentTopicService tournamentTopicService) : ControllerBase
{
    private readonly ITournamentService _tournamentService = tournamentService;
    private readonly IMatchService _matchService = matchService;
    private readonly IRoundService _roundService = roundService;
    private readonly ITournamentParticipantService _participantService = participantService;
    private readonly ITournamentTopicService _tournamentTopicService = tournamentTopicService;

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        List<GetTournamentDto> tournamentsDto = await _tournamentService.GetTournamentsAsync();

        return Ok(tournamentsDto);
    }

    [HttpGet]
    [Route("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        Result<GetTournamentDto> result = await _tournamentService.GetTournamentAsync(id);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateTournamentDto createTournamentDto)
    {
        Result<GetTournamentDto> result = await _tournamentService.CreateTournamentAsync(createTournamentDto);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        GetTournamentDto tournamentDto = result.Value;
        return CreatedAtAction(nameof(GetById), new { id = tournamentDto.Id }, tournamentDto);
    }

    [HttpPut]
    [Route("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateTournamentDto updateTournamentDto)
    {
        Result<GetTournamentDto> result = await _tournamentService.UpdateTournamentAsync(id, updateTournamentDto);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpDelete]
    [Route("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        Result result = await _tournamentService.DeleteTournamentAsync(id);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return NoContent();
    }

    [HttpPatch]
    [Route("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> AdvanceStage([FromRoute] Guid id, [FromBody] UpdateTournamentStageDto advanceStageDto)
    {
        Result<GetTournamentDto> result = await _tournamentService.AdvanceStageAsync(id, advanceStageDto);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }
}
