using Cbo.API.Contracts.Matches;
using Cbo.API.Cqrs;
using Cbo.API.ErrorHandling;
using Cbo.API.Queries.Matches;
using Cbo.Results;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Endpoints;

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
}
