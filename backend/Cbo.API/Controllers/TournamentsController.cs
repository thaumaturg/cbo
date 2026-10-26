using Cbo.API.Commands.Tournaments;
using Cbo.API.Cqrs;
using Cbo.API.ErrorHandling;
using Cbo.API.Models.Constants;
using Cbo.API.Models.DTO;
using Cbo.API.Queries.Tournaments;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public partial class TournamentsController(
    IMatchService matchService,
    IRoundService roundService,
    ITournamentTopicService tournamentTopicService) : ControllerBase
{
    private readonly IMatchService _matchService = matchService;
    private readonly IRoundService _roundService = roundService;
    private readonly ITournamentTopicService _tournamentTopicService = tournamentTopicService;

    [HttpGet]
    [Authorize]
    [ProducesResponseType<List<GetTournamentDto>>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromServices] IQueryHandler<GetTournamentsQuery, List<GetTournamentDto>> getTournaments,
        CancellationToken cancellationToken)
    {
        Result<List<GetTournamentDto>> result = await getTournaments.HandleAsync(new GetTournamentsQuery(), cancellationToken);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpGet]
    [Route("{id:guid}")]
    [Authorize]
    [ProducesResponseType<GetTournamentDto>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        [FromServices] IQueryHandler<GetTournamentQuery, GetTournamentDto> getTournament,
        CancellationToken cancellationToken)
    {
        Result<GetTournamentDto> result = await getTournament.HandleAsync(new GetTournamentQuery(id), cancellationToken);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType<GetTournamentDto>(StatusCodes.Status201Created)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTournamentDto createTournamentDto,
        [FromServices] ICommandHandler<CreateTournamentCommand, Guid> createTournament,
        [FromServices] IQueryHandler<GetTournamentQuery, GetTournamentDto> getTournament,
        CancellationToken cancellationToken)
    {
        var command = new CreateTournamentCommand(
            createTournamentDto.Title,
            createTournamentDto.Description,
            createTournamentDto.PlayersPerTournament,
            createTournamentDto.TopicsPerParticipantMax,
            createTournamentDto.TopicsPerParticipantMin);

        Result<Guid> created = await createTournament.HandleAsync(command, cancellationToken);
        if (created.IsFailure)
            return this.Problem(created.Failure);

        Result<GetTournamentDto> tournament = await getTournament.HandleAsync(new GetTournamentQuery(created.Value), cancellationToken);
        if (tournament.IsFailure)
            return this.Problem(tournament.Failure);

        return CreatedAtAction(nameof(GetById), new { id = tournament.Value.Id }, tournament.Value);
    }

    [HttpPut]
    [Route("{id:guid}")]
    [Authorize]
    [ProducesResponseType<GetTournamentDto>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateTournamentDto updateTournamentDto,
        [FromServices] ICommandHandler<UpdateTournamentCommand> updateTournament,
        [FromServices] IQueryHandler<GetTournamentQuery, GetTournamentDto> getTournament,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTournamentCommand(
            id,
            updateTournamentDto.Title,
            updateTournamentDto.Description,
            updateTournamentDto.PlayersPerTournament,
            updateTournamentDto.TopicsPerParticipantMax,
            updateTournamentDto.TopicsPerParticipantMin);

        Result updated = await updateTournament.HandleAsync(command, cancellationToken);
        if (updated.IsFailure)
            return this.Problem(updated.Failure);

        Result<GetTournamentDto> tournament = await getTournament.HandleAsync(new GetTournamentQuery(id), cancellationToken);
        if (tournament.IsFailure)
            return this.Problem(tournament.Failure);

        return Ok(tournament.Value);
    }

    [HttpDelete]
    [Route("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        [FromServices] ICommandHandler<DeleteTournamentCommand> deleteTournament,
        CancellationToken cancellationToken)
    {
        Result result = await deleteTournament.HandleAsync(new DeleteTournamentCommand(id), cancellationToken);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return NoContent();
    }

    [HttpPatch]
    [Route("{id:guid}")]
    [Authorize]
    [ProducesResponseType<GetTournamentDto>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Start(
        [FromRoute] Guid id,
        [FromBody] UpdateTournamentStageDto updateTournamentStageDto,
        [FromServices] ICommandHandler<StartTournamentCommand> startTournament,
        [FromServices] IQueryHandler<GetTournamentQuery, GetTournamentDto> getTournament,
        CancellationToken cancellationToken)
    {
        // Keeps the PATCH { stage } shape: starting is the only supported stage change
        if (updateTournamentStageDto.Stage != TournamentStage.Qualifications)
            return this.Problem(Failure.Invalid(
                "Some of the stage data is invalid.",
                new FieldError("stage", "Only advancing to the Qualifications stage is currently supported.")));

        Result started = await startTournament.HandleAsync(new StartTournamentCommand(id), cancellationToken);
        if (started.IsFailure)
            return this.Problem(started.Failure);

        Result<GetTournamentDto> tournament = await getTournament.HandleAsync(new GetTournamentQuery(id), cancellationToken);
        if (tournament.IsFailure)
            return this.Problem(tournament.Failure);

        return Ok(tournament.Value);
    }
}
