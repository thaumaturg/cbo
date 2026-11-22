using Cbo.Api.ErrorHandling;
using Cbo.Core.Cqrs;
using Cbo.Core.Friends;
using Cbo.Results;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.Api.Endpoints;

public static class FriendEndpoints
{
    public static RouteGroupBuilder MapFriendEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder friends = api.MapGroup("Friends").WithTags("Friends");

        // The whole list, not a page (ADR-0009)
        friends.MapGet("", GetFriendsAsync)
            .WithName("GetFriends")
            .Produces<List<FriendResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        friends.MapPost("", AddFriendAsync)
            .WithName("AddFriend")
            .Produces<FriendResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesValidationProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        friends.MapDelete("{accountId:guid}", RemoveFriendAsync)
            .WithName("RemoveFriend")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<IResult> GetFriendsAsync(
        [FromServices] IQueryHandler<GetFriendsQuery, List<FriendResponse>> getFriends,
        CancellationToken cancellationToken)
    {
        Result<List<FriendResponse>> result = await getFriends.HandleAsync(new GetFriendsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> AddFriendAsync(
        AddFriendRequest addFriendRequest,
        [FromServices] ICommandHandler<AddFriendCommand, Guid> addFriend,
        [FromServices] IQueryHandler<GetFriendQuery, FriendResponse> getFriend,
        CancellationToken cancellationToken)
    {
        Result<Guid> added = await addFriend.HandleAsync(new AddFriendCommand(addFriendRequest.Username), cancellationToken);
        if (added.IsFailure)
            return added.Failure.ToProblem();

        Result<FriendResponse> friend = await getFriend.HandleAsync(new GetFriendQuery(added.Value), cancellationToken);
        if (friend.IsFailure)
            return friend.Failure.ToProblem();

        // No endpoint reads a single friend, so there's no Location header
        return TypedResults.Created((string?)null, friend.Value);
    }

    private static async Task<IResult> RemoveFriendAsync(
        Guid accountId,
        [FromServices] ICommandHandler<RemoveFriendCommand> removeFriend,
        CancellationToken cancellationToken)
    {
        Result result = await removeFriend.HandleAsync(new RemoveFriendCommand(accountId), cancellationToken);
        if (result.IsFailure)
            return result.Failure.ToProblem();

        return TypedResults.NoContent();
    }
}
