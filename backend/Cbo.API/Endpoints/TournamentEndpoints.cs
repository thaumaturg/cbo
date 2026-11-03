using Cbo.API.Commands.Tournaments;
using Cbo.API.Contracts.Tournaments;
using Cbo.API.Cqrs;
using Cbo.API.ErrorHandling;
using Cbo.API.Models.Constants;
using Cbo.API.Queries.Tournaments;
using Cbo.Results;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Endpoints;

public static class TournamentEndpoints
{
    public static RouteGroupBuilder MapTournamentEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder tournaments = api.MapGroup("Tournaments").WithTags("Tournaments");

        tournaments.MapGet("", GetTournamentsAsync)
            .WithName("GetTournaments")
            .Produces<List<TournamentResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        tournaments.MapGet("{id:guid}", GetTournamentAsync)
            .WithName("GetTournament")
            .Produces<TournamentResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        tournaments.MapPost("", CreateTournamentAsync)
            .WithName("CreateTournament")
            .Produces<TournamentResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        tournaments.MapPut("{id:guid}", UpdateTournamentAsync)
            .WithName("UpdateTournament")
            .Produces<TournamentResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        tournaments.MapDelete("{id:guid}", DeleteTournamentAsync)
            .WithName("DeleteTournament")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        tournaments.MapPatch("{id:guid}", StartTournamentAsync)
            .WithName("StartTournament")
            .Produces<TournamentResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return api;
    }

    private static async Task<IResult> GetTournamentsAsync(
        [FromServices] IQueryHandler<GetTournamentsQuery, List<TournamentResponse>> getTournaments,
        CancellationToken cancellationToken)
    {
        Result<List<TournamentResponse>> result = await getTournaments.HandleAsync(new GetTournamentsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> GetTournamentAsync(
        Guid id,
        [FromServices] IQueryHandler<GetTournamentQuery, TournamentResponse> getTournament,
        CancellationToken cancellationToken)
    {
        Result<TournamentResponse> result = await getTournament.HandleAsync(new GetTournamentQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> CreateTournamentAsync(
        CreateTournamentRequest createTournamentRequest,
        [FromServices] ICommandHandler<CreateTournamentCommand, Guid> createTournament,
        [FromServices] IQueryHandler<GetTournamentQuery, TournamentResponse> getTournament,
        CancellationToken cancellationToken)
    {
        var command = new CreateTournamentCommand(
            createTournamentRequest.Title,
            createTournamentRequest.Description,
            createTournamentRequest.PlayersPerTournament,
            createTournamentRequest.TopicsPerParticipantMax,
            createTournamentRequest.TopicsPerParticipantMin);

        Result<Guid> created = await createTournament.HandleAsync(command, cancellationToken);
        if (created.IsFailure)
            return created.Failure.ToProblem();

        Result<TournamentResponse> tournament = await getTournament.HandleAsync(new GetTournamentQuery(created.Value), cancellationToken);
        if (tournament.IsFailure)
            return tournament.Failure.ToProblem();

        return TypedResults.CreatedAtRoute(tournament.Value, "GetTournament", new { id = tournament.Value.Id });
    }

    private static async Task<IResult> UpdateTournamentAsync(
        Guid id,
        UpdateTournamentRequest updateTournamentRequest,
        [FromServices] ICommandHandler<UpdateTournamentCommand> updateTournament,
        [FromServices] IQueryHandler<GetTournamentQuery, TournamentResponse> getTournament,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTournamentCommand(
            id,
            updateTournamentRequest.Title,
            updateTournamentRequest.Description,
            updateTournamentRequest.PlayersPerTournament,
            updateTournamentRequest.TopicsPerParticipantMax,
            updateTournamentRequest.TopicsPerParticipantMin);

        Result updated = await updateTournament.HandleAsync(command, cancellationToken);
        if (updated.IsFailure)
            return updated.Failure.ToProblem();

        Result<TournamentResponse> tournament = await getTournament.HandleAsync(new GetTournamentQuery(id), cancellationToken);
        if (tournament.IsFailure)
            return tournament.Failure.ToProblem();

        return TypedResults.Ok(tournament.Value);
    }

    private static async Task<IResult> DeleteTournamentAsync(
        Guid id,
        [FromServices] ICommandHandler<DeleteTournamentCommand> deleteTournament,
        CancellationToken cancellationToken)
    {
        Result result = await deleteTournament.HandleAsync(new DeleteTournamentCommand(id), cancellationToken);
        if (result.IsFailure)
            return result.Failure.ToProblem();

        return TypedResults.NoContent();
    }

    private static async Task<IResult> StartTournamentAsync(
        Guid id,
        StartTournamentRequest startTournamentRequest,
        [FromServices] ICommandHandler<StartTournamentCommand> startTournament,
        [FromServices] IQueryHandler<GetTournamentQuery, TournamentResponse> getTournament,
        CancellationToken cancellationToken)
    {
        // Keeps the PATCH { stage } shape: starting is the only supported stage change
        if (startTournamentRequest.Stage != TournamentStage.Qualifications)
            return Failure.Invalid(
                "Some of the stage data is invalid.",
                new FieldError("stage", "Only advancing to the Qualifications stage is currently supported.")).ToProblem();

        Result started = await startTournament.HandleAsync(new StartTournamentCommand(id), cancellationToken);
        if (started.IsFailure)
            return started.Failure.ToProblem();

        Result<TournamentResponse> tournament = await getTournament.HandleAsync(new GetTournamentQuery(id), cancellationToken);
        if (tournament.IsFailure)
            return tournament.Failure.ToProblem();

        return TypedResults.Ok(tournament.Value);
    }
}
