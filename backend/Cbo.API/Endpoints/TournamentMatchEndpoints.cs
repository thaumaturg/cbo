using Cbo.API.Cqrs;
using Cbo.API.ErrorHandling;
using Cbo.API.Models.DTO;
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
            .Produces<List<GetMatchDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        matches.MapGet("{matchId:guid}", GetTournamentMatchAsync)
            .WithName("GetTournamentMatch")
            .Produces<GetMatchDetailDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        matches.MapGet("{matchId:guid}/available-topics", GetMatchAvailableTopicsAsync)
            .WithName("GetMatchAvailableTopics")
            .Produces<List<GetAvailableTopicDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<IResult> GetTournamentMatchesAsync(
        Guid tournamentId,
        [FromServices] IQueryHandler<GetMatchesQuery, List<GetMatchDto>> getMatches,
        CancellationToken cancellationToken)
    {
        Result<List<GetMatchDto>> result = await getMatches.HandleAsync(new GetMatchesQuery(tournamentId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> GetTournamentMatchAsync(
        Guid tournamentId,
        Guid matchId,
        [FromServices] IQueryHandler<GetMatchQuery, GetMatchDetailDto> getMatch,
        CancellationToken cancellationToken)
    {
        Result<GetMatchDetailDto> result = await getMatch.HandleAsync(new GetMatchQuery(tournamentId, matchId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> GetMatchAvailableTopicsAsync(
        Guid tournamentId,
        Guid matchId,
        [FromServices] IQueryHandler<GetAvailableTopicsQuery, List<GetAvailableTopicDto>> getAvailableTopics,
        CancellationToken cancellationToken)
    {
        Result<List<GetAvailableTopicDto>> result = await getAvailableTopics.HandleAsync(
            new GetAvailableTopicsQuery(tournamentId, matchId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }
}
