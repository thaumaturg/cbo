using Cbo.API.Models.Domain;
using Cbo.API.Repositories;
using Cbo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Cbo.API.Authorization;

public class TopicAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Topic>
{
    private readonly ITopicAuthorRepository _topicAuthorRepository;
    private readonly ICurrentUserService _currentUserService;

    public TopicAuthorizationHandler(
        ITopicAuthorRepository topicAuthorRepository,
        ICurrentUserService currentUserService)
    {
        _topicAuthorRepository = topicAuthorRepository;
        _currentUserService = currentUserService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Topic resource)
    {
        ApplicationUser? user = await _currentUserService.GetCurrentUserAsync();
        if (user is null)
            return;

        TopicAuthor? topicAuthor = await _topicAuthorRepository.GetByUserIdAndTopicIdAsync(user.Id, resource.Id);

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
