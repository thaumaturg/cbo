using Cbo.API.Commands.TournamentTopics;
using Cbo.API.Cqrs;
using Cbo.API.ErrorHandling;
using Cbo.API.Models.DTO;
using Cbo.API.Queries.TournamentTopics;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

public partial class TournamentsController
{
    [HttpGet]
    [Route("{tournamentId:guid}/topics")]
    [Authorize]
    [ProducesResponseType<List<GetTournamentTopicDto>>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyTopics(
        [FromRoute] Guid tournamentId,
        [FromServices] IQueryHandler<GetMyTournamentTopicsQuery, List<GetTournamentTopicDto>> getMyTopics,
        CancellationToken cancellationToken)
    {
        Result<List<GetTournamentTopicDto>> result = await getMyTopics.HandleAsync(
            new GetMyTournamentTopicsQuery(tournamentId), cancellationToken);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpGet]
    [Route("{tournamentId:guid}/topics/all")]
    [Authorize]
    [ProducesResponseType<List<GetTournamentTopicDto>>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAllTopics(
        [FromRoute] Guid tournamentId,
        [FromServices] IQueryHandler<GetAllTournamentTopicsQuery, List<GetTournamentTopicDto>> getAllTopics,
        CancellationToken cancellationToken)
    {
        Result<List<GetTournamentTopicDto>> result = await getAllTopics.HandleAsync(
            new GetAllTournamentTopicsQuery(tournamentId), cancellationToken);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpPut]
    [Route("{tournamentId:guid}/topics")]
    [Authorize]
    [ProducesResponseType<List<GetTournamentTopicDto>>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetMyTopics(
        [FromRoute] Guid tournamentId,
        [FromBody] List<UpdateTournamentTopicDto> topicsDto,
        [FromServices] ICommandHandler<SetMyTournamentTopicsCommand> setMyTopics,
        [FromServices] IQueryHandler<GetMyTournamentTopicsQuery, List<GetTournamentTopicDto>> getMyTopics,
        CancellationToken cancellationToken)
    {
        var command = new SetMyTournamentTopicsCommand(
            tournamentId,
            topicsDto.Select(dto => new ChosenTopic(dto.TopicId, dto.PriorityIndex)).ToList());

        Result updated = await setMyTopics.HandleAsync(command, cancellationToken);
        if (updated.IsFailure)
            return this.Problem(updated.Failure);

        Result<List<GetTournamentTopicDto>> topics = await getMyTopics.HandleAsync(
            new GetMyTournamentTopicsQuery(tournamentId), cancellationToken);
        if (topics.IsFailure)
            return this.Problem(topics.Failure);

        return Ok(topics.Value);
    }

    [HttpGet]
    [Route("{tournamentId:guid}/topics/{topicId:guid}")]
    [Authorize]
    [ProducesResponseType<GetTopicDto>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTournamentTopicById(
        [FromRoute] Guid tournamentId,
        [FromRoute] Guid topicId,
        [FromServices] IQueryHandler<GetTournamentTopicQuery, GetTopicDto> getTournamentTopic,
        CancellationToken cancellationToken)
    {
        Result<GetTopicDto> result = await getTournamentTopic.HandleAsync(
            new GetTournamentTopicQuery(tournamentId, topicId), cancellationToken);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }
}
