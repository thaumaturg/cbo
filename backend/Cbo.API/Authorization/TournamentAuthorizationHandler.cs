using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.API.Repositories;
using Cbo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Cbo.API.Authorization;

public class TournamentAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Tournament>
{
    private readonly ITournamentParticipantsRepository _participantsRepository;
    private readonly ICurrentUserService _currentUserService;

    public TournamentAuthorizationHandler(
        ITournamentParticipantsRepository participantsRepository,
        ICurrentUserService currentUserService)
    {
        _participantsRepository = participantsRepository;
        _currentUserService = currentUserService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Tournament resource)
    {
        ApplicationUser? user = await _currentUserService.GetCurrentUserAsync();
        if (user is null)
            return;

        TournamentParticipant? participant = await _participantsRepository.GetByUserIdAndTournamentIdAsync(user.Id, resource.Id);

        if (participant is null)
            return;

        if (requirement.Name == TournamentOperations.Read.Name)
        {
            context.Succeed(requirement);
            return;
        }

        if (requirement.Name == TournamentOperations.Update.Name ||
            requirement.Name == TournamentOperations.Delete.Name ||
            requirement.Name == TournamentOperations.ManageParticipants.Name ||
            requirement.Name == TournamentOperations.AdvanceStage.Name ||
            requirement.Name == TournamentOperations.ManageRounds.Name)
        {
            if (participant.Role == TournamentParticipantRole.Creator)
            {
                context.Succeed(requirement);
            }
            return;
        }

        if (requirement.Name == TournamentOperations.ViewAllTopics.Name)
        {
            if (participant.Role == TournamentParticipantRole.Creator ||
                participant.Role == TournamentParticipantRole.Organizer)
            {
                context.Succeed(requirement);
            }
        }
    }
}
