using System.Security.Claims;
using Cbo.Api.Data;
using Cbo.Api.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Api.Authorization;

public class TopicAuthorizationHandler(
    CboDbContext dbContext) : AuthorizationHandler<OperationAuthorizationRequirement, Topic>
{
    private readonly CboDbContext _dbContext = dbContext;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Topic resource)
    {
        if (!Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId))
            return;

        TopicAuthor? topicAuthor = await _dbContext.TopicAuthors
            .AsNoTracking()
            .FirstOrDefaultAsync(ta => ta.ApplicationUserId == userId && ta.TopicId == resource.Id);

        if (requirement.Name == TopicOperations.Read.Name ||
            requirement.Name == TopicOperations.Update.Name ||
            requirement.Name == TopicOperations.Delete.Name ||
            requirement.Name == TopicOperations.ManageAuthors.Name)
        {
            if (topicAuthor is not null && topicAuthor.IsOwner)
            {
                context.Succeed(requirement);
            }
        }
    }
}
