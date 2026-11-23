using Cbo.Api.ErrorHandling;
using Cbo.Core.Cqrs;
using Cbo.Core.Paging;
using Cbo.Core.Topics;
using Cbo.Results;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.Api.Endpoints;

public static class TopicEndpoints
{
    // Kestrel answers a larger body with 413; 50 full topics take a fraction of it
    private const long ImportBodyMaxBytes = 1_048_576;

    public static RouteGroupBuilder MapTopicEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder topics = api.MapGroup("Topics").WithTags("Topics");

        topics.MapGet("", GetTopicsAsync)
            .WithName("GetTopics")
            .Produces<PageResponse<TopicListItemResponse>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        topics.MapGet("{id:guid}", GetTopicAsync)
            .WithName("GetTopic")
            .Produces<TopicResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        topics.MapPost("", CreateTopicAsync)
            .WithName("CreateTopic")
            .Produces<TopicResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);

        topics.MapPost("import", ImportTopicsAsync)
            .WithName("ImportTopics")
            .WithMetadata(new RequestSizeLimitAttribute(ImportBodyMaxBytes))
            .Produces<ImportTopicsResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge);

        topics.MapPut("{id:guid}", UpdateTopicAsync)
            .WithName("UpdateTopic")
            .Produces<TopicResponse>()
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
        [AsParameters] GetTopicsRequest getTopicsRequest,
        [FromServices] IQueryHandler<GetTopicsQuery, PageResponse<TopicListItemResponse>> getTopics,
        CancellationToken cancellationToken)
    {
        var query = new GetTopicsQuery(
            getTopicsRequest.Page ?? PagingDefaults.FirstPage,
            getTopicsRequest.PageSize ?? PagingDefaults.DefaultPageSize,
            getTopicsRequest.Sort,
            getTopicsRequest.Search,
            getTopicsRequest.Played,
            getTopicsRequest.IsAuthor);

        Result<PageResponse<TopicListItemResponse>> result = await getTopics.HandleAsync(query, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> GetTopicAsync(
        Guid id,
        [FromServices] IQueryHandler<GetTopicQuery, TopicResponse> getTopic,
        CancellationToken cancellationToken)
    {
        Result<TopicResponse> result = await getTopic.HandleAsync(new GetTopicQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> CreateTopicAsync(
        CreateTopicRequest createTopicRequest,
        [FromServices] ICommandHandler<CreateTopicCommand, Guid> createTopic,
        [FromServices] IQueryHandler<GetTopicQuery, TopicResponse> getTopic,
        CancellationToken cancellationToken)
    {
        var command = new CreateTopicCommand(
            createTopicRequest.Title,
            createTopicRequest.Description,
            createTopicRequest.IsAuthor,
            createTopicRequest.Questions
                .Select(q => new NewQuestion(q.QuestionNumber, q.Cost, q.Text, q.Answer, q.Comment))
                .ToList(),
            createTopicRequest.Authors.Select(a => a.Username).ToList());

        Result<Guid> created = await createTopic.HandleAsync(command, cancellationToken);
        if (created.IsFailure)
            return created.Failure.ToProblem();

        Result<TopicResponse> topic = await getTopic.HandleAsync(new GetTopicQuery(created.Value), cancellationToken);
        if (topic.IsFailure)
            return topic.Failure.ToProblem();

        return TypedResults.CreatedAtRoute(topic.Value, "GetTopic", new { id = topic.Value.Id });
    }

    private static async Task<IResult> ImportTopicsAsync(
        ImportTopicsRequest importTopicsRequest,
        [FromServices] ICommandHandler<ImportTopicsCommand, int> importTopics,
        CancellationToken cancellationToken)
    {
        var command = new ImportTopicsCommand(
            importTopicsRequest.IsAuthor,
            importTopicsRequest.Topics
                .Select(t => new ImportedTopic(
                    t.Title,
                    t.Description,
                    t.Questions
                        .Select(q => new NewQuestion(q.QuestionNumber, q.Cost, q.Text, q.Answer, q.Comment))
                        .ToList()))
                .ToList());

        Result<int> imported = await importTopics.HandleAsync(command, cancellationToken);
        if (imported.IsFailure)
            return imported.Failure.ToProblem();

        // The topics have no single location, so the response carries no Location header
        return TypedResults.Created((string?)null, new ImportTopicsResponse { Count = imported.Value });
    }

    private static async Task<IResult> UpdateTopicAsync(
        Guid id,
        UpdateTopicRequest updateTopicRequest,
        [FromServices] ICommandHandler<UpdateTopicCommand> updateTopic,
        [FromServices] IQueryHandler<GetTopicQuery, TopicResponse> getTopic,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTopicCommand(
            id,
            updateTopicRequest.Title,
            updateTopicRequest.Description,
            updateTopicRequest.IsAuthor,
            updateTopicRequest.Questions
                .Select(q => new EditedQuestion(q.Id, q.QuestionNumber, q.Cost, q.Text, q.Answer, q.Comment))
                .ToList(),
            updateTopicRequest.Authors.Select(a => a.Username).ToList());

        Result updated = await updateTopic.HandleAsync(command, cancellationToken);
        if (updated.IsFailure)
            return updated.Failure.ToProblem();

        Result<TopicResponse> topic = await getTopic.HandleAsync(new GetTopicQuery(id), cancellationToken);
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
