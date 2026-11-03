using Cbo.API.Commands.Rounds;
using Cbo.API.Contracts.Rounds;
using Cbo.API.Cqrs;
using Cbo.API.ErrorHandling;
using Cbo.API.Queries.Rounds;
using Cbo.Results;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Endpoints;

public static class TournamentRoundEndpoints
{
    public static RouteGroupBuilder MapTournamentRoundEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder rounds = api.MapGroup("Tournaments/{tournamentId:guid}/matches/{matchId:guid}/rounds").WithTags("Tournaments");

        rounds.MapGet("{roundNumber:int}", GetMatchRoundAsync)
            .WithName("GetMatchRound")
            .Produces<RoundResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        rounds.MapPost("", CreateMatchRoundAsync)
            .WithName("CreateMatchRound")
            .Produces<RoundResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        rounds.MapPut("{roundNumber:int}", UpdateMatchRoundAsync)
            .WithName("UpdateMatchRound")
            .Produces<RoundResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        rounds.MapDelete("{roundNumber:int}", DeleteMatchRoundAsync)
            .WithName("DeleteMatchRound")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<IResult> GetMatchRoundAsync(
        Guid tournamentId,
        Guid matchId,
        int roundNumber,
        [FromServices] IQueryHandler<GetRoundQuery, RoundResponse> getRound,
        CancellationToken cancellationToken)
    {
        Result<RoundResponse> result = await getRound.HandleAsync(new GetRoundQuery(tournamentId, matchId, roundNumber), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> CreateMatchRoundAsync(
        Guid tournamentId,
        Guid matchId,
        CreateRoundRequest createRoundRequest,
        [FromServices] ICommandHandler<CreateRoundCommand> createRound,
        [FromServices] IQueryHandler<GetRoundQuery, RoundResponse> getRound,
        CancellationToken cancellationToken)
    {
        var command = new CreateRoundCommand(
            tournamentId,
            matchId,
            createRoundRequest.NumberInMatch,
            createRoundRequest.TopicId,
            createRoundRequest.IsOverrideMode,
            createRoundRequest.Answers
                .Select(a => new GivenAnswer(a.QuestionId, a.MatchParticipantId, a.IsAnswerAccepted, a.OverrideCost))
                .ToList());

        Result created = await createRound.HandleAsync(command, cancellationToken);
        if (created.IsFailure)
            return created.Failure.ToProblem();

        Result<RoundResponse> round = await getRound.HandleAsync(
            new GetRoundQuery(tournamentId, matchId, createRoundRequest.NumberInMatch), cancellationToken);
        if (round.IsFailure)
            return round.Failure.ToProblem();

        return TypedResults.CreatedAtRoute(
            round.Value, "GetMatchRound", new { tournamentId, matchId, roundNumber = round.Value.NumberInMatch });
    }

    private static async Task<IResult> UpdateMatchRoundAsync(
        Guid tournamentId,
        Guid matchId,
        int roundNumber,
        UpdateRoundRequest updateRoundRequest,
        [FromServices] ICommandHandler<UpdateRoundCommand> updateRound,
        [FromServices] IQueryHandler<GetRoundQuery, RoundResponse> getRound,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRoundCommand(
            tournamentId,
            matchId,
            roundNumber,
            updateRoundRequest.NumberInMatch,
            updateRoundRequest.TopicId,
            updateRoundRequest.IsOverrideMode,
            updateRoundRequest.Answers
                .Select(a => new GivenAnswer(a.QuestionId, a.MatchParticipantId, a.IsAnswerAccepted, a.OverrideCost))
                .ToList());

        Result updated = await updateRound.HandleAsync(command, cancellationToken);
        if (updated.IsFailure)
            return updated.Failure.ToProblem();

        Result<RoundResponse> round = await getRound.HandleAsync(new GetRoundQuery(tournamentId, matchId, roundNumber), cancellationToken);
        if (round.IsFailure)
            return round.Failure.ToProblem();

        return TypedResults.Ok(round.Value);
    }

    private static async Task<IResult> DeleteMatchRoundAsync(
        Guid tournamentId,
        Guid matchId,
        int roundNumber,
        [FromServices] ICommandHandler<DeleteRoundCommand> deleteRound,
        CancellationToken cancellationToken)
    {
        Result result = await deleteRound.HandleAsync(new DeleteRoundCommand(tournamentId, matchId, roundNumber), cancellationToken);
        if (result.IsFailure)
            return result.Failure.ToProblem();

        return TypedResults.NoContent();
    }
}
