using Cbo.API.Authorization;
using Cbo.API.Commands.Participants;
using Cbo.API.Cqrs;
using Cbo.API.Data;
using Cbo.API.Data.Configuration;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.CommandHandlers.Participants;

public sealed class CreateParticipantCommandHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService,
    UserManager<ApplicationUser> userManager) : CommandHandler<CreateParticipantCommand, Guid>(dbContext)
{
    private const string ManageParticipantsForbiddenMessage = "You don't have permission to manage the participants of this tournament.";

    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    protected override async Task<Result<Guid>> ExecuteAsync(CreateParticipantCommand command, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            command.TournamentId, TournamentOperations.ManageParticipants, ManageParticipantsForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        if (command.Role == TournamentParticipantRole.Creator)
            return ParticipantFailures.InvalidField("role", "A tournament has only one creator, so nobody can be added as a creator.");

        ApplicationUser? user = await _userManager.FindByNameAsync(command.Username);
        if (user is null)
            return ParticipantFailures.InvalidField("username", $"User '{command.Username}' doesn't exist.");

        if (access.Value.CurrentStage != TournamentStage.Preparations)
            return ParticipantFailures.Locked();

        if (command.Role == TournamentParticipantRole.Organizer
            && await ParticipantRules.HasMaxOrganizersAsync(DbContext, command.TournamentId, cancellationToken))
            return ParticipantFailures.TooManyOrganizers();

        bool isAlreadyAParticipant = await DbContext.TournamentParticipants
            .AnyAsync(tp => tp.ApplicationUserId == user.Id && tp.TournamentId == command.TournamentId, cancellationToken);
        if (isAlreadyAParticipant)
            return ParticipantFailures.AlreadyAParticipant(command.Username);

        // Set here rather than by the database default, so the id is known before the save
        var participant = new TournamentParticipant
        {
            Id = Guid.CreateVersion7(),
            Role = command.Role,
            TournamentId = command.TournamentId,
            ApplicationUserId = user.Id,
        };

        if (command.Role == TournamentParticipantRole.Player)
            participant.Seed = await ParticipantRules.GetNextPlayerSeedAsync(DbContext, command.TournamentId, cancellationToken);

        DbContext.TournamentParticipants.Add(participant);

        return participant.Id;
    }

    protected override Failure? MapSaveFailure(CreateParticipantCommand command, DbUpdateException exception)
    {
        // Lost a race with a simultaneous add of the same user
        if (PostgresConstraint.IsUniqueViolation(exception, TournamentParticipantConfiguration.TournamentIdApplicationUserIdIndexName))
            return ParticipantFailures.AlreadyAParticipant(command.Username);

        return null;
    }
}
