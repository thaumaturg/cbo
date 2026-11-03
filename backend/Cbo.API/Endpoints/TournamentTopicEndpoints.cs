using Cbo.API.Commands.TournamentTopics;
using Cbo.API.Contracts.Topics;
using Cbo.API.Cqrs;
using Cbo.API.ErrorHandling;
using Cbo.API.Models.DTO;
using Cbo.API.Queries.TournamentTopics;
using Cbo.Results;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Endpoints;

public static class TournamentTopicEndpoints
{
    public static RouteGroupBuilder MapTournamentTopicEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder topics = api.MapGroup("Tournaments/{tournamentId:guid}/topics").WithTags("Tournaments");

        topics.MapGet("", GetMyTournamentTopicsAsync)
            .WithName("GetMyTournamentTopics")
            .Produces<List<TournamentTopicResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        topics.MapGet("all", GetAllTournamentTopicsAsync)
            .WithName("GetAllTournamentTopics")
            .Produces<List<TournamentTopicResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        topics.MapPut("", SetMyTournamentTopicsAsync)
            .WithName("SetMyTournamentTopics")
            .Produces<List<TournamentTopicResponse>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        topics.MapGet("{topicId:guid}", GetTournamentTopicAsync)
            .WithName("GetTournamentTopic")
            .Produces<TopicResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<IResult> GetMyTournamentTopicsAsync(
        Guid tournamentId,
        [FromServices] IQueryHandler<GetMyTournamentTopicsQuery, List<TournamentTopicResponse>> getMyTopics,
        CancellationToken cancellationToken)
    {
        Result<List<TournamentTopicResponse>> result = await getMyTopics.HandleAsync(
            new GetMyTournamentTopicsQuery(tournamentId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> GetAllTournamentTopicsAsync(
        Guid tournamentId,
        [FromServices] IQueryHandler<GetAllTournamentTopicsQuery, List<TournamentTopicResponse>> getAllTopics,
        CancellationToken cancellationToken)
    {
        Result<List<TournamentTopicResponse>> result = await getAllTopics.HandleAsync(
            new GetAllTournamentTopicsQuery(tournamentId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> SetMyTournamentTopicsAsync(
        Guid tournamentId,
        List<ChosenTopicRequest> chosenTopics,
        [FromServices] ICommandHandler<SetMyTournamentTopicsCommand> setMyTopics,
        [FromServices] IQueryHandler<GetMyTournamentTopicsQuery, List<TournamentTopicResponse>> getMyTopics,
        CancellationToken cancellationToken)
    {
        var command = new SetMyTournamentTopicsCommand(
            tournamentId,
            chosenTopics.Select(topic => new ChosenTopic(topic.TopicId, topic.PriorityIndex)).ToList());

        Result updated = await setMyTopics.HandleAsync(command, cancellationToken);
        if (updated.IsFailure)
            return updated.Failure.ToProblem();

        Result<List<TournamentTopicResponse>> topics = await getMyTopics.HandleAsync(
            new GetMyTournamentTopicsQuery(tournamentId), cancellationToken);
        if (topics.IsFailure)
            return topics.Failure.ToProblem();

        return TypedResults.Ok(topics.Value);
    }

    private static async Task<IResult> GetTournamentTopicAsync(
        Guid tournamentId,
        Guid topicId,
        [FromServices] IQueryHandler<GetTournamentTopicQuery, TopicResponse> getTournamentTopic,
        CancellationToken cancellationToken)
    {
        Result<TopicResponse> result = await getTournamentTopic.HandleAsync(
            new GetTournamentTopicQuery(tournamentId, topicId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }
}
