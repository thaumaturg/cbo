using Cbo.API.ErrorHandling;
using Cbo.API.Models.Constants;
using Cbo.API.Models.DTO;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

public partial class TournamentsController
{
    [HttpGet]
    [Route("{tournamentId:guid}/participants")]
    [Authorize]
    [ProducesResponseType<List<GetTournamentParticipantDto>>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAllParticipants(
        [FromRoute] Guid tournamentId,
        [FromQuery] TournamentParticipantRole? role = null)
    {
        Result<List<GetTournamentParticipantDto>> result = await _participantService.GetParticipantsAsync(tournamentId, role);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpGet]
    [Route("{tournamentId:guid}/participants/{id:guid}")]
    [Authorize]
    [ProducesResponseType<GetTournamentParticipantDto>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetParticipantById([FromRoute] Guid tournamentId, [FromRoute] Guid id)
    {
        Result<GetTournamentParticipantDto> result = await _participantService.GetParticipantAsync(tournamentId, id);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpPost]
    [Route("{tournamentId:guid}/participants")]
    [Authorize]
    [ProducesResponseType<GetTournamentParticipantDto>(StatusCodes.Status201Created)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateParticipant([FromRoute] Guid tournamentId, [FromBody] CreateTournamentParticipantDto createParticipantDto)
    {
        Result<GetTournamentParticipantDto> result = await _participantService.CreateParticipantAsync(tournamentId, createParticipantDto);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        GetTournamentParticipantDto participantDto = result.Value;
        return CreatedAtAction(nameof(GetParticipantById), new { tournamentId, id = participantDto.Id }, participantDto);
    }

    [HttpPut]
    [Route("{tournamentId:guid}/participants/{id:guid}")]
    [Authorize]
    [ProducesResponseType<GetTournamentParticipantDto>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateParticipant([FromRoute] Guid tournamentId, [FromRoute] Guid id, [FromBody] UpdateTournamentParticipantDto updateParticipantDto)
    {
        Result<GetTournamentParticipantDto> result = await _participantService.UpdateParticipantAsync(tournamentId, id, updateParticipantDto);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpPut]
    [Route("{tournamentId:guid}/participants/seeding")]
    [Authorize]
    [ProducesResponseType<List<GetTournamentParticipantDto>>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateParticipantsSeeding([FromRoute] Guid tournamentId, [FromBody] UpdateParticipantsSeedingDto seedingDto)
    {
        Result<List<GetTournamentParticipantDto>> result = await _participantService.UpdateSeedingAsync(tournamentId, seedingDto);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpDelete]
    [Route("{tournamentId:guid}/participants/{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status400BadRequest)]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteParticipant([FromRoute] Guid tournamentId, [FromRoute] Guid id)
    {
        Result result = await _participantService.DeleteParticipantAsync(tournamentId, id);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return NoContent();
    }
}
