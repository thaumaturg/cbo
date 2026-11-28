using Cbo.Api.ErrorHandling;
using Cbo.Core.Cqrs;
using Cbo.Core.Domain;
using Cbo.Core.Paging;
using Cbo.Core.Tournaments;
using Cbo.Results;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.Api.Endpoints;

public static class TournamentEndpoints
{
    public static RouteGroupBuilder MapTournamentEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder tournaments = api.MapGroup("Tournaments").WithTags("Tournaments");

        tournaments.MapGet("", GetTournamentsAsync)
            .WithName("GetTournaments")
            .Produces<PageResponse<TournamentResponse>>()
            .ProducesValidationProblem()
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

        tournaments.MapPatch("{id:guid}", ChangeTournamentStageAsync)
            .WithName("ChangeTournamentStage")
            .Produces<TournamentResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return api;
    }

    private static async Task<IResult> GetTournamentsAsync(
        [AsParameters] GetTournamentsRequest getTournamentsRequest,
        [FromServices] IQueryHandler<GetTournamentsQuery, PageResponse<TournamentResponse>> getTournaments,
        CancellationToken cancellationToken)
    {
        var query = new GetTournamentsQuery(
            getTournamentsRequest.Page ?? PagingDefaults.FirstPage,
            getTournamentsRequest.PageSize ?? PagingDefaults.DefaultPageSize,
            getTournamentsRequest.Sort,
            getTournamentsRequest.Search,
            getTournamentsRequest.Stage,
            getTournamentsRequest.Role ?? []);

        Result<PageResponse<TournamentResponse>> result = await getTournaments.HandleAsync(query, cancellationToken);
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
            createTournamentRequest.Format,
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
            updateTournamentRequest.Format,
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

    private static async Task<IResult> ChangeTournamentStageAsync(
        Guid id,
        ChangeStageRequest changeStageRequest,
        [FromServices] ICommandHandler<StartQualificationsCommand> startQualifications,
        [FromServices] ICommandHandler<StartPlayoffCommand> startPlayoff,
        [FromServices] ICommandHandler<EndTournamentCommand> endTournament,
        [FromServices] ICommandHandler<RollBackCommand> rollBack,
        [FromServices] IQueryHandler<GetTournamentQuery, TournamentResponse> getTournament,
        CancellationToken cancellationToken)
    {
        Result<TournamentResponse> current = await getTournament.HandleAsync(new GetTournamentQuery(id), cancellationToken);
        if (current.IsFailure)
            return current.Failure.ToProblem();

        // One stage-move endpoint: Preparations, or any stage before the current one, is a roll back; any other stage
        // picks its forward move. Each handler checks the stage again under the tournament's lock, so a move that
        // raced this read is rejected
        TournamentStage stage = changeStageRequest.Stage;
        bool rollsBack = stage == TournamentStage.Preparations || stage < Enum.Parse<TournamentStage>(current.Value.CurrentStage);
        Task<Result> move = rollsBack
            ? rollBack.HandleAsync(new RollBackCommand(id, stage), cancellationToken)
            : stage switch
            {
                TournamentStage.Qualifications => startQualifications.HandleAsync(new StartQualificationsCommand(id), cancellationToken),
                TournamentStage.Playoff => startPlayoff.HandleAsync(new StartPlayoffCommand(id), cancellationToken),
                TournamentStage.Ended => endTournament.HandleAsync(new EndTournamentCommand(id), cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(changeStageRequest), stage, "No move to this stage."),
            };

        Result moved = await move;
        if (moved.IsFailure)
            return moved.Failure.ToProblem();

        Result<TournamentResponse> tournament = await getTournament.HandleAsync(new GetTournamentQuery(id), cancellationToken);
        if (tournament.IsFailure)
            return tournament.Failure.ToProblem();

        return TypedResults.Ok(tournament.Value);
    }
}
