using Cbo.API.Commands.Topics;
using Cbo.API.Cqrs;
using Cbo.API.ErrorHandling;
using Cbo.API.Models.DTO;
using Cbo.API.Queries.Topics;
using Cbo.Results;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Endpoints;

public static class TopicAuthorEndpoints
{
    public static RouteGroupBuilder MapTopicAuthorEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder authors = api.MapGroup("Topics/{topicId:guid}/authors").WithTags("Topics");

        authors.MapGet("", GetTopicAuthorsAsync)
            .WithName("GetTopicAuthors")
            .Produces<List<GetTopicAuthorDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        authors.MapGet("{id:guid}", GetTopicAuthorAsync)
            .WithName("GetTopicAuthor")
            .Produces<GetTopicAuthorDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        authors.MapPost("", CreateTopicAuthorAsync)
            .WithName("CreateTopicAuthor")
            .Produces<GetTopicAuthorDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        authors.MapDelete("{id:guid}", DeleteTopicAuthorAsync)
            .WithName("DeleteTopicAuthor")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return api;
    }

    private static async Task<IResult> GetTopicAuthorsAsync(
        Guid topicId,
        [FromServices] IQueryHandler<GetTopicAuthorsQuery, List<GetTopicAuthorDto>> getAuthors,
        CancellationToken cancellationToken)
    {
        Result<List<GetTopicAuthorDto>> result = await getAuthors.HandleAsync(new GetTopicAuthorsQuery(topicId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> GetTopicAuthorAsync(
        Guid topicId,
        Guid id,
        [FromServices] IQueryHandler<GetTopicAuthorQuery, GetTopicAuthorDto> getAuthor,
        CancellationToken cancellationToken)
    {
        Result<GetTopicAuthorDto> result = await getAuthor.HandleAsync(new GetTopicAuthorQuery(topicId, id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> CreateTopicAuthorAsync(
        Guid topicId,
        CreateTopicAuthorDto createAuthorDto,
        [FromServices] ICommandHandler<CreateTopicAuthorCommand, Guid> createAuthor,
        [FromServices] IQueryHandler<GetTopicAuthorQuery, GetTopicAuthorDto> getAuthor,
        CancellationToken cancellationToken)
    {
        Result<Guid> created = await createAuthor.HandleAsync(
            new CreateTopicAuthorCommand(topicId, createAuthorDto.Username), cancellationToken);
        if (created.IsFailure)
            return created.Failure.ToProblem();

        Result<GetTopicAuthorDto> author = await getAuthor.HandleAsync(
            new GetTopicAuthorQuery(topicId, created.Value), cancellationToken);
        if (author.IsFailure)
            return author.Failure.ToProblem();

        return TypedResults.CreatedAtRoute(author.Value, "GetTopicAuthor", new { topicId, id = author.Value.Id });
    }

    private static async Task<IResult> DeleteTopicAuthorAsync(
        Guid topicId,
        Guid id,
        [FromServices] ICommandHandler<DeleteTopicAuthorCommand> deleteAuthor,
        CancellationToken cancellationToken)
    {
        Result result = await deleteAuthor.HandleAsync(new DeleteTopicAuthorCommand(topicId, id), cancellationToken);
        if (result.IsFailure)
            return result.Failure.ToProblem();

        return TypedResults.NoContent();
    }
}
