using Cbo.API.Commands.Topics;
using Cbo.API.Cqrs;
using Cbo.API.ErrorHandling;
using Cbo.API.Models.DTO;
using Cbo.API.Queries.Topics;
using Cbo.Results;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Endpoints;

public static class TopicEndpoints
{
    public static RouteGroupBuilder MapTopicEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder topics = api.MapGroup("Topics").WithTags("Topics");

        topics.MapGet("", GetTopicsAsync)
            .WithName("GetTopics")
            .Produces<List<GetTopicDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        topics.MapGet("{id:guid}", GetTopicAsync)
            .WithName("GetTopic")
            .Produces<GetTopicDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        topics.MapPost("", CreateTopicAsync)
            .WithName("CreateTopic")
            .Produces<GetTopicDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        topics.MapPut("{id:guid}", UpdateTopicAsync)
            .WithName("UpdateTopic")
            .Produces<GetTopicDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        topics.MapDelete("{id:guid}", DeleteTopicAsync)
            .WithName("DeleteTopic")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return api;
    }

    private static async Task<IResult> GetTopicsAsync(
        [FromServices] IQueryHandler<GetTopicsQuery, List<GetTopicDto>> getTopics,
        CancellationToken cancellationToken)
    {
        Result<List<GetTopicDto>> result = await getTopics.HandleAsync(new GetTopicsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> GetTopicAsync(
        Guid id,
        [FromServices] IQueryHandler<GetTopicQuery, GetTopicDto> getTopic,
        CancellationToken cancellationToken)
    {
        Result<GetTopicDto> result = await getTopic.HandleAsync(new GetTopicQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> CreateTopicAsync(
        CreateTopicDto createTopicDto,
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
            return created.Failure.ToProblem();

        Result<GetTopicDto> topic = await getTopic.HandleAsync(new GetTopicQuery(created.Value), cancellationToken);
        if (topic.IsFailure)
            return topic.Failure.ToProblem();

        return TypedResults.CreatedAtRoute(topic.Value, "GetTopic", new { id = topic.Value.Id });
    }

    private static async Task<IResult> UpdateTopicAsync(
        Guid id,
        UpdateTopicDto updateTopicDto,
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
            return updated.Failure.ToProblem();

        Result<GetTopicDto> topic = await getTopic.HandleAsync(new GetTopicQuery(id), cancellationToken);
        if (topic.IsFailure)
            return topic.Failure.ToProblem();

        return TypedResults.Ok(topic.Value);
    }

    private static async Task<IResult> DeleteTopicAsync(
        Guid id,
        [FromServices] ICommandHandler<DeleteTopicCommand> deleteTopic,
        CancellationToken cancellationToken)
    {
        Result result = await deleteTopic.HandleAsync(new DeleteTopicCommand(id), cancellationToken);
        if (result.IsFailure)
            return result.Failure.ToProblem();

        return TypedResults.NoContent();
    }
}
