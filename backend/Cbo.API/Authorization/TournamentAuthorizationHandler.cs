using System.Security.Claims;
using Cbo.API.Data;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Authorization;

public class TournamentAuthorizationHandler(
    CboDbContext dbContext) : AuthorizationHandler<OperationAuthorizationRequirement, Tournament>
{
    private readonly CboDbContext _dbContext = dbContext;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Tournament resource)
    {
        if (!Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId))
            return;

        TournamentParticipant? participant = await _dbContext.TournamentParticipants
            .AsNoTracking()
            .FirstOrDefaultAsync(tp => tp.ApplicationUserId == userId && tp.TournamentId == resource.Id);

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
