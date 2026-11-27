using Cbo.Api.ErrorHandling;
using Cbo.Core.Cqrs;
using Cbo.Core.Matches;
using Cbo.Core.Playoffs;
using Cbo.Results;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.Api.Endpoints;

public static class TournamentMatchEndpoints
{
    public static RouteGroupBuilder MapTournamentMatchEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder matches = api.MapGroup("Tournaments/{tournamentId:guid}/matches").WithTags("Tournaments");

        matches.MapGet("", GetTournamentMatchesAsync)
            .WithName("GetTournamentMatches")
            .Produces<List<MatchResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        matches.MapGet("{matchId:guid}", GetTournamentMatchAsync)
            .WithName("GetTournamentMatch")
            .Produces<MatchDetailResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        matches.MapGet("{matchId:guid}/available-topics", GetMatchAvailableTopicsAsync)
            .WithName("GetMatchAvailableTopics")
            .Produces<List<AvailableTopicResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        matches.MapPost("{matchId:guid}/confirm", ConfirmTournamentMatchAsync)
            .WithName("ConfirmTournamentMatch")
            .Produces<MatchDetailResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return api;
    }

    private static async Task<IResult> GetTournamentMatchesAsync(
        Guid tournamentId,
        [FromServices] IQueryHandler<GetMatchesQuery, List<MatchResponse>> getMatches,
        CancellationToken cancellationToken)
    {
        Result<List<MatchResponse>> result = await getMatches.HandleAsync(new GetMatchesQuery(tournamentId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> GetTournamentMatchAsync(
        Guid tournamentId,
        Guid matchId,
        [FromServices] IQueryHandler<GetMatchQuery, MatchDetailResponse> getMatch,
        CancellationToken cancellationToken)
    {
        Result<MatchDetailResponse> result = await getMatch.HandleAsync(new GetMatchQuery(tournamentId, matchId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> GetMatchAvailableTopicsAsync(
        Guid tournamentId,
        Guid matchId,
        [FromServices] IQueryHandler<GetAvailableTopicsQuery, List<AvailableTopicResponse>> getAvailableTopics,
        CancellationToken cancellationToken)
    {
        Result<List<AvailableTopicResponse>> result = await getAvailableTopics.HandleAsync(
            new GetAvailableTopicsQuery(tournamentId, matchId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> ConfirmTournamentMatchAsync(
        Guid tournamentId,
        Guid matchId,
        [FromServices] ICommandHandler<ConfirmMatchCommand> confirmMatch,
        [FromServices] IQueryHandler<GetMatchQuery, MatchDetailResponse> getMatch,
        CancellationToken cancellationToken)
    {
        Result confirmed = await confirmMatch.HandleAsync(new ConfirmMatchCommand(tournamentId, matchId), cancellationToken);
        if (confirmed.IsFailure)
            return confirmed.Failure.ToProblem();

        Result<MatchDetailResponse> match = await getMatch.HandleAsync(new GetMatchQuery(tournamentId, matchId), cancellationToken);
        return match.IsSuccess ? TypedResults.Ok(match.Value) : match.Failure.ToProblem();
    }
}
