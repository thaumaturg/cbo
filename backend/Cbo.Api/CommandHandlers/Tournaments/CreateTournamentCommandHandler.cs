using Cbo.Api.Commands.Tournaments;
using Cbo.Api.Cqrs;
using Cbo.Api.Data;
using Cbo.Api.Models.Constants;
using Cbo.Api.Models.Domain;
using Cbo.Api.Services;
using Cbo.Results;

namespace Cbo.Api.CommandHandlers.Tournaments;

public sealed class CreateTournamentCommandHandler(
    CboDbContext dbContext,
    ICurrentUserService currentUserService) : CommandHandler<CreateTournamentCommand, Guid>(dbContext)
{
    private const string InvalidTournamentMessage = "Some of the tournament data is invalid.";

    private readonly ICurrentUserService _currentUserService = currentUserService;

    protected override Task<Result<Guid>> ExecuteAsync(CreateTournamentCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(CreateTournament(command));

    private Result<Guid> CreateTournament(CreateTournamentCommand command)
    {
        TournamentSettings settings = TournamentSettings.Resolve(
            command.PlayersPerTournament,
            command.TopicsPerParticipantMax,
            command.TopicsPerParticipantMin);

        List<FieldError> fieldErrors = settings.Validate();
        if (fieldErrors.Count > 0)
            return Failure.Invalid(InvalidTournamentMessage, fieldErrors);

        Guid creatorId = _currentUserService.GetRequiredCurrentUserId();

        // Set here rather than by the database default, so the id is known before the save
        var tournament = new Tournament
        {
            Id = Guid.CreateVersion7(),
            Title = command.Title,
            Description = command.Description,
            CurrentStage = TournamentStage.Preparations,
            PlayersPerTournament = settings.PlayersPerTournament,
            TopicsPerParticipantMax = settings.TopicsPerParticipantMax,
            TopicsPerParticipantMin = settings.TopicsPerParticipantMin,
        };

        tournament.TournamentParticipants.Add(new TournamentParticipant
        {
            Role = TournamentParticipantRole.Creator,
            PointsSum = null,
            ApplicationUserId = creatorId,
        });

        DbContext.Tournaments.Add(tournament);

        return tournament.Id;
    }
}
