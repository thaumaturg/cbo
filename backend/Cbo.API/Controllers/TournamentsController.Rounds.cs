using Cbo.API.Commands.Rounds;
using Cbo.API.Cqrs;
using Cbo.API.ErrorHandling;
using Cbo.API.Models.DTO;
using Cbo.API.Queries.Rounds;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

public partial class TournamentsController
{
    [HttpGet]
    [Route("{tournamentId:guid}/matches/{matchId:guid}/rounds/{roundNumber:int}")]
    [Authorize]
    [ProducesResponseType<GetRoundDto>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRound(
        [FromRoute] Guid tournamentId,
        [FromRoute] Guid matchId,
        [FromRoute] int roundNumber,
        [FromServices] IQueryHandler<GetRoundQuery, GetRoundDto> getRound,
        CancellationToken cancellationToken)
    {
        Result<GetRoundDto> result = await getRound.HandleAsync(new GetRoundQuery(tournamentId, matchId, roundNumber), cancellationToken);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpPost]
    [Route("{tournamentId:guid}/matches/{matchId:guid}/rounds")]
    [Authorize]
    [ProducesResponseType<GetRoundDto>(StatusCodes.Status201Created)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRound(
        [FromRoute] Guid tournamentId,
        [FromRoute] Guid matchId,
        [FromBody] CreateRoundWithAnswersDto createRoundDto,
        [FromServices] ICommandHandler<CreateRoundCommand> createRound,
        [FromServices] IQueryHandler<GetRoundQuery, GetRoundDto> getRound,
        CancellationToken cancellationToken)
    {
        var command = new CreateRoundCommand(
            tournamentId,
            matchId,
            createRoundDto.NumberInMatch,
            createRoundDto.TopicId,
            createRoundDto.IsOverrideMode,
            ToGivenAnswers(createRoundDto.Answers));

        Result created = await createRound.HandleAsync(command, cancellationToken);
        if (created.IsFailure)
            return this.Problem(created.Failure);

        Result<GetRoundDto> round = await getRound.HandleAsync(
            new GetRoundQuery(tournamentId, matchId, createRoundDto.NumberInMatch), cancellationToken);
        if (round.IsFailure)
            return this.Problem(round.Failure);

        return CreatedAtAction(nameof(GetRound), new { tournamentId, matchId, roundNumber = round.Value.NumberInMatch }, round.Value);
    }

    [HttpPut]
    [Route("{tournamentId:guid}/matches/{matchId:guid}/rounds/{roundNumber:int}")]
    [Authorize]
    [ProducesResponseType<GetRoundDto>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateRound(
        [FromRoute] Guid tournamentId,
        [FromRoute] Guid matchId,
        [FromRoute] int roundNumber,
        [FromBody] CreateRoundWithAnswersDto updateRoundDto,
        [FromServices] ICommandHandler<UpdateRoundCommand> updateRound,
        [FromServices] IQueryHandler<GetRoundQuery, GetRoundDto> getRound,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRoundCommand(
            tournamentId,
            matchId,
            roundNumber,
            updateRoundDto.NumberInMatch,
            updateRoundDto.TopicId,
            updateRoundDto.IsOverrideMode,
            ToGivenAnswers(updateRoundDto.Answers));

        Result updated = await updateRound.HandleAsync(command, cancellationToken);
        if (updated.IsFailure)
            return this.Problem(updated.Failure);

        Result<GetRoundDto> round = await getRound.HandleAsync(new GetRoundQuery(tournamentId, matchId, roundNumber), cancellationToken);
        if (round.IsFailure)
            return this.Problem(round.Failure);

        return Ok(round.Value);
    }

    [HttpDelete]
    [Route("{tournamentId:guid}/matches/{matchId:guid}/rounds/{roundNumber:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRound(
        [FromRoute] Guid tournamentId,
        [FromRoute] Guid matchId,
        [FromRoute] int roundNumber,
        [FromServices] ICommandHandler<DeleteRoundCommand> deleteRound,
        CancellationToken cancellationToken)
    {
        Result result = await deleteRound.HandleAsync(new DeleteRoundCommand(tournamentId, matchId, roundNumber), cancellationToken);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return NoContent();
    }

    private static List<GivenAnswer> ToGivenAnswers(List<CreateRoundAnswerDto> answers) =>
        answers
            .Select(a => new GivenAnswer(a.QuestionId, a.MatchParticipantId, a.IsAnswerAccepted, a.OverrideCost))
            .ToList();
}
