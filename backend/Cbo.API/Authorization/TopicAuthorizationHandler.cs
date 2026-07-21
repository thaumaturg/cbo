using System.Security.Claims;
using Cbo.API.Models.Domain;
using Cbo.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Cbo.API.Authorization;

public class TopicAuthorizationHandler(
    ITopicAuthorRepository topicAuthorRepository) : AuthorizationHandler<OperationAuthorizationRequirement, Topic>
{
    private readonly ITopicAuthorRepository _topicAuthorRepository = topicAuthorRepository;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Topic resource)
    {
        if (!Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId))
            return;

        TopicAuthor? topicAuthor = await _topicAuthorRepository.GetByUserIdAndTopicIdAsync(userId, resource.Id);

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
