using Cbo.API.Commands.Topics;
using Cbo.API.Cqrs;
using Cbo.API.ErrorHandling;
using Cbo.API.Models.DTO;
using Cbo.API.Queries.Topics;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public partial class TopicsController : ControllerBase
{
    [HttpGet]
    [Authorize]
    [ProducesResponseType<List<GetTopicDto>>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromServices] IQueryHandler<GetTopicsQuery, List<GetTopicDto>> getTopics,
        CancellationToken cancellationToken)
    {
        Result<List<GetTopicDto>> result = await getTopics.HandleAsync(new GetTopicsQuery(), cancellationToken);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpGet]
    [Route("{id:guid}")]
    [Authorize]
    [ProducesResponseType<GetTopicDto>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        [FromServices] IQueryHandler<GetTopicQuery, GetTopicDto> getTopic,
        CancellationToken cancellationToken)
    {
        Result<GetTopicDto> result = await getTopic.HandleAsync(new GetTopicQuery(id), cancellationToken);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType<GetTopicDto>(StatusCodes.Status201Created)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTopicDto createTopicDto,
        [FromServices] ICommandHandler<CreateTopicCommand, Guid> createTopic,
        [FromServices] IQueryHandler<GetTopicQuery, GetTopicDto> getTopic,
        CancellationToken cancellationToken)
    {
        var command = new CreateTopicCommand(
            createTopicDto.Title,
            createTopicDto.Description,
            createTopicDto.IsAuthor,
            createTopicDto.Questions
                .Select(q => new NewQuestion(q.QuestionNumber, q.CostPositive, q.CostNegative, q.Text, q.Answer, q.Comment))
                .ToList());

        Result<Guid> created = await createTopic.HandleAsync(command, cancellationToken);
        if (created.IsFailure)
            return this.Problem(created.Failure);

        Result<GetTopicDto> topic = await getTopic.HandleAsync(new GetTopicQuery(created.Value), cancellationToken);
        if (topic.IsFailure)
            return this.Problem(topic.Failure);

        return CreatedAtAction(nameof(GetById), new { id = topic.Value.Id }, topic.Value);
    }

    [HttpPut]
    [Route("{id:guid}")]
    [Authorize]
    [ProducesResponseType<GetTopicDto>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateTopicDto updateTopicDto,
        [FromServices] ICommandHandler<UpdateTopicCommand> updateTopic,
        [FromServices] IQueryHandler<GetTopicQuery, GetTopicDto> getTopic,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTopicCommand(
            id,
            updateTopicDto.Title,
            updateTopicDto.Description,
            updateTopicDto.IsAuthor,
            updateTopicDto.Questions
                .Select(q => new EditedQuestion(q.Id, q.QuestionNumber, q.CostPositive, q.CostNegative, q.Text, q.Answer, q.Comment))
                .ToList());

        Result updated = await updateTopic.HandleAsync(command, cancellationToken);
        if (updated.IsFailure)
            return this.Problem(updated.Failure);

        Result<GetTopicDto> topic = await getTopic.HandleAsync(new GetTopicQuery(id), cancellationToken);
        if (topic.IsFailure)
            return this.Problem(topic.Failure);

        return Ok(topic.Value);
    }

    [HttpDelete]
    [Route("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        [FromServices] ICommandHandler<DeleteTopicCommand> deleteTopic,
        CancellationToken cancellationToken)
    {
        Result result = await deleteTopic.HandleAsync(new DeleteTopicCommand(id), cancellationToken);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return NoContent();
    }
}
