using Cbo.API.Commands.Topics;
using Cbo.API.Cqrs;
using Cbo.API.ErrorHandling;
using Cbo.API.Models.DTO;
using Cbo.API.Queries.Topics;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Controllers;

public partial class TopicsController
{
    [HttpGet]
    [Route("{topicId:guid}/authors")]
    [Authorize]
    [ProducesResponseType<List<GetTopicAuthorDto>>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAllAuthors(
        [FromRoute] Guid topicId,
        [FromServices] IQueryHandler<GetTopicAuthorsQuery, List<GetTopicAuthorDto>> getAuthors,
        CancellationToken cancellationToken)
    {
        Result<List<GetTopicAuthorDto>> result = await getAuthors.HandleAsync(new GetTopicAuthorsQuery(topicId), cancellationToken);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpGet]
    [Route("{topicId:guid}/authors/{id:guid}")]
    [Authorize]
    [ProducesResponseType<GetTopicAuthorDto>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuthorById(
        [FromRoute] Guid topicId,
        [FromRoute] Guid id,
        [FromServices] IQueryHandler<GetTopicAuthorQuery, GetTopicAuthorDto> getAuthor,
        CancellationToken cancellationToken)
    {
        Result<GetTopicAuthorDto> result = await getAuthor.HandleAsync(new GetTopicAuthorQuery(topicId, id), cancellationToken);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return Ok(result.Value);
    }

    [HttpPost]
    [Route("{topicId:guid}/authors")]
    [Authorize]
    [ProducesResponseType<GetTopicAuthorDto>(StatusCodes.Status201Created)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAuthor(
        [FromRoute] Guid topicId,
        [FromBody] CreateTopicAuthorDto createAuthorDto,
        [FromServices] ICommandHandler<CreateTopicAuthorCommand, Guid> createAuthor,
        [FromServices] IQueryHandler<GetTopicAuthorQuery, GetTopicAuthorDto> getAuthor,
        CancellationToken cancellationToken)
    {
        Result<Guid> created = await createAuthor.HandleAsync(
            new CreateTopicAuthorCommand(topicId, createAuthorDto.Username), cancellationToken);
        if (created.IsFailure)
            return this.Problem(created.Failure);

        Result<GetTopicAuthorDto> author = await getAuthor.HandleAsync(
            new GetTopicAuthorQuery(topicId, created.Value), cancellationToken);
        if (author.IsFailure)
            return this.Problem(author.Failure);

        return CreatedAtAction(nameof(GetAuthorById), new { topicId, id = author.Value.Id }, author.Value);
    }

    [HttpDelete]
    [Route("{topicId:guid}/authors/{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status400BadRequest)]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAuthor(
        [FromRoute] Guid topicId,
        [FromRoute] Guid id,
        [FromServices] ICommandHandler<DeleteTopicAuthorCommand> deleteAuthor,
        CancellationToken cancellationToken)
    {
        Result result = await deleteAuthor.HandleAsync(new DeleteTopicAuthorCommand(topicId, id), cancellationToken);
        if (result.IsFailure)
            return this.Problem(result.Failure);

        return NoContent();
    }
}
