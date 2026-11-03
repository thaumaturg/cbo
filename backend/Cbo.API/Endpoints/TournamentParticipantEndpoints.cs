using Cbo.API.Commands.Participants;
using Cbo.API.Contracts.Participants;
using Cbo.API.Cqrs;
using Cbo.API.ErrorHandling;
using Cbo.API.Models.Constants;
using Cbo.API.Queries.Participants;
using Cbo.Results;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Endpoints;

public static class TournamentParticipantEndpoints
{
    public static RouteGroupBuilder MapTournamentParticipantEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder participants = api.MapGroup("Tournaments/{tournamentId:guid}/participants").WithTags("Tournaments");

        participants.MapGet("", GetTournamentParticipantsAsync)
            .WithName("GetTournamentParticipants")
            .Produces<List<ParticipantResponse>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        participants.MapGet("{id:guid}", GetTournamentParticipantAsync)
            .WithName("GetTournamentParticipant")
            .Produces<ParticipantResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        participants.MapPost("", CreateTournamentParticipantAsync)
            .WithName("CreateTournamentParticipant")
            .Produces<ParticipantResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        participants.MapPut("{id:guid}", UpdateTournamentParticipantAsync)
            .WithName("UpdateTournamentParticipant")
            .Produces<ParticipantResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        participants.MapPut("seeding", UpdateTournamentSeedingAsync)
            .WithName("UpdateTournamentSeeding")
            .Produces<List<ParticipantResponse>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        participants.MapDelete("{id:guid}", DeleteTournamentParticipantAsync)
            .WithName("DeleteTournamentParticipant")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return api;
    }

    private static async Task<IResult> GetTournamentParticipantsAsync(
        Guid tournamentId,
        TournamentParticipantRole? role,
        [FromServices] IQueryHandler<GetParticipantsQuery, List<ParticipantResponse>> getParticipants,
        CancellationToken cancellationToken)
    {
        Result<List<ParticipantResponse>> result = await getParticipants.HandleAsync(
            new GetParticipantsQuery(tournamentId, role), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> GetTournamentParticipantAsync(
        Guid tournamentId,
        Guid id,
        [FromServices] IQueryHandler<GetParticipantQuery, ParticipantResponse> getParticipant,
        CancellationToken cancellationToken)
    {
        Result<ParticipantResponse> result = await getParticipant.HandleAsync(
            new GetParticipantQuery(tournamentId, id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> CreateTournamentParticipantAsync(
        Guid tournamentId,
        CreateParticipantRequest createParticipantRequest,
        [FromServices] ICommandHandler<CreateParticipantCommand, Guid> createParticipant,
        [FromServices] IQueryHandler<GetParticipantQuery, ParticipantResponse> getParticipant,
        CancellationToken cancellationToken)
    {
        var command = new CreateParticipantCommand(tournamentId, createParticipantRequest.Username, createParticipantRequest.Role);

        Result<Guid> created = await createParticipant.HandleAsync(command, cancellationToken);
        if (created.IsFailure)
            return created.Failure.ToProblem();

        Result<ParticipantResponse> participant = await getParticipant.HandleAsync(
            new GetParticipantQuery(tournamentId, created.Value), cancellationToken);
        if (participant.IsFailure)
            return participant.Failure.ToProblem();

        return TypedResults.CreatedAtRoute(
            participant.Value, "GetTournamentParticipant", new { tournamentId, id = participant.Value.Id });
    }

    private static async Task<IResult> UpdateTournamentParticipantAsync(
        Guid tournamentId,
        Guid id,
        UpdateParticipantRequest updateParticipantRequest,
        [FromServices] ICommandHandler<UpdateParticipantCommand> updateParticipant,
        [FromServices] IQueryHandler<GetParticipantQuery, ParticipantResponse> getParticipant,
        CancellationToken cancellationToken)
    {
        Result updated = await updateParticipant.HandleAsync(
            new UpdateParticipantCommand(tournamentId, id, updateParticipantRequest.Role), cancellationToken);
        if (updated.IsFailure)
            return updated.Failure.ToProblem();

        Result<ParticipantResponse> participant = await getParticipant.HandleAsync(
            new GetParticipantQuery(tournamentId, id), cancellationToken);
        if (participant.IsFailure)
            return participant.Failure.ToProblem();

        return TypedResults.Ok(participant.Value);
    }

    private static async Task<IResult> UpdateTournamentSeedingAsync(
        Guid tournamentId,
        UpdateSeedingRequest seedingRequest,
        [FromServices] ICommandHandler<UpdateSeedingCommand> updateSeeding,
        [FromServices] IQueryHandler<GetParticipantsQuery, List<ParticipantResponse>> getParticipants,
        CancellationToken cancellationToken)
    {
        Result updated = await updateSeeding.HandleAsync(
            new UpdateSeedingCommand(tournamentId, seedingRequest.OrderedParticipantIds), cancellationToken);
        if (updated.IsFailure)
            return updated.Failure.ToProblem();

        Result<List<ParticipantResponse>> players = await getParticipants.HandleAsync(
            new GetParticipantsQuery(tournamentId, TournamentParticipantRole.Player), cancellationToken);
        if (players.IsFailure)
            return players.Failure.ToProblem();

        return TypedResults.Ok(players.Value);
    }

    private static async Task<IResult> DeleteTournamentParticipantAsync(
        Guid tournamentId,
        Guid id,
        [FromServices] ICommandHandler<DeleteParticipantCommand> deleteParticipant,
        CancellationToken cancellationToken)
    {
        Result result = await deleteParticipant.HandleAsync(new DeleteParticipantCommand(tournamentId, id), cancellationToken);
        if (result.IsFailure)
            return result.Failure.ToProblem();

        return TypedResults.NoContent();
    }
}
