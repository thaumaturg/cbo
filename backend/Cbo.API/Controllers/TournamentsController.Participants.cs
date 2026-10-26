using Cbo.API.Commands.Participants;
using Cbo.API.Cqrs;
using Cbo.API.ErrorHandling;
using Cbo.API.Models.Constants;
using Cbo.API.Models.DTO;
using Cbo.API.Queries.Participants;
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
        [FromServices] IQueryHandler<GetParticipantsQuery, List<GetTournamentParticipantDto>> getParticipants,
        CancellationToken cancellationToken,
        [FromQuery] TournamentParticipantRole? role = null)
    {
        Result<List<GetTournamentParticipantDto>> result = await getParticipants.HandleAsync(
            new GetParticipantsQuery(tournamentId, role), cancellationToken);
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
    public async Task<IActionResult> GetParticipantById(
        [FromRoute] Guid tournamentId,
        [FromRoute] Guid id,
        [FromServices] IQueryHandler<GetParticipantQuery, GetTournamentParticipantDto> getParticipant,
        CancellationToken cancellationToken)
    {
        Result<GetTournamentParticipantDto> result = await getParticipant.HandleAsync(
            new GetParticipantQuery(tournamentId, id), cancellationToken);
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
    public async Task<IActionResult> CreateParticipant(
        [FromRoute] Guid tournamentId,
        [FromBody] CreateTournamentParticipantDto createParticipantDto,
        [FromServices] ICommandHandler<CreateParticipantCommand, Guid> createParticipant,
        [FromServices] IQueryHandler<GetParticipantQuery, GetTournamentParticipantDto> getParticipant,
        CancellationToken cancellationToken)
    {
        var command = new CreateParticipantCommand(tournamentId, createParticipantDto.Username, createParticipantDto.Role);

        Result<Guid> created = await createParticipant.HandleAsync(command, cancellationToken);
        if (created.IsFailure)
            return this.Problem(created.Failure);

        Result<GetTournamentParticipantDto> participant = await getParticipant.HandleAsync(
            new GetParticipantQuery(tournamentId, created.Value), cancellationToken);
        if (participant.IsFailure)
            return this.Problem(participant.Failure);

        return CreatedAtAction(nameof(GetParticipantById), new { tournamentId, id = participant.Value.Id }, participant.Value);
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
    public async Task<IActionResult> UpdateParticipant(
        [FromRoute] Guid tournamentId,
        [FromRoute] Guid id,
        [FromBody] UpdateTournamentParticipantDto updateParticipantDto,
        [FromServices] ICommandHandler<UpdateParticipantCommand> updateParticipant,
        [FromServices] IQueryHandler<GetParticipantQuery, GetTournamentParticipantDto> getParticipant,
        CancellationToken cancellationToken)
    {
        Result updated = await updateParticipant.HandleAsync(
            new UpdateParticipantCommand(tournamentId, id, updateParticipantDto.Role), cancellationToken);
        if (updated.IsFailure)
            return this.Problem(updated.Failure);

        Result<GetTournamentParticipantDto> participant = await getParticipant.HandleAsync(
            new GetParticipantQuery(tournamentId, id), cancellationToken);
        if (participant.IsFailure)
            return this.Problem(participant.Failure);

        return Ok(participant.Value);
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
    public async Task<IActionResult> UpdateParticipantsSeeding(
        [FromRoute] Guid tournamentId,
        [FromBody] UpdateParticipantsSeedingDto seedingDto,
        [FromServices] ICommandHandler<UpdateSeedingCommand> updateSeeding,
        [FromServices] IQueryHandler<GetParticipantsQuery, List<GetTournamentParticipantDto>> getParticipants,
        CancellationToken cancellationToken)
    {
        Result updated = await updateSeeding.HandleAsync(
            new UpdateSeedingCommand(tournamentId, seedingDto.OrderedParticipantIds), cancellationToken);
        if (updated.IsFailure)
            return this.Problem(updated.Failure);

        Result<List<GetTournamentParticipantDto>> players = await getParticipants.HandleAsync(
            new GetParticipantsQuery(tournamentId, TournamentParticipantRole.Player), cancellationToken);
        if (players.IsFailure)
            return this.Problem(players.Failure);

        return Ok(players.Value);
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
    public async Task<IActionResult> DeleteParticipant(
        [FromRoute] Guid tournamentId,
        [FromRoute] Guid id,
        [FromServices] ICommandHandler<DeleteParticipantCommand> deleteParticipant,
        CancellationToken cancellationToken)
    {
        Result result = await deleteParticipant.HandleAsync(new DeleteParticipantCommand(tournamentId, id), cancellationToken);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return NoContent();
    }
}
