using Cbo.API.Data;
using Cbo.API.Models.Domain;
using Cbo.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.Services;

public interface ITopicAccessService
{
    /// <summary>
    /// Loads the topic from the URL, tracked so command handlers can change it, and checks the current user's permission on it.
    /// Its navigations aren't loaded.
    /// </summary>
    Task<Result<Topic>> LoadTopicWithAccessAsync(
        Guid topicId,
        OperationAuthorizationRequirement operation,
        string forbiddenMessage,
        CancellationToken cancellationToken);
}

public class TopicAccessService(
    CboDbContext dbContext,
    IAuthorizationService authorizationService,
    ICurrentUserService currentUserService) : ITopicAccessService
{
    private readonly CboDbContext _dbContext = dbContext;
    private readonly IAuthorizationService _authorizationService = authorizationService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<Topic>> LoadTopicWithAccessAsync(
        Guid topicId,
        OperationAuthorizationRequirement operation,
        string forbiddenMessage,
        CancellationToken cancellationToken)
    {
        Topic? topic = await _dbContext.Topics
            .FirstOrDefaultAsync(t => t.Id == topicId, cancellationToken);
        if (topic is null)
            return Failure.NotFound("Topic not found.");

        AuthorizationResult authResult = await _authorizationService.AuthorizeAsync(_currentUserService.GetRequiredCurrentUser(), topic, operation);
        if (!authResult.Succeeded)
            return Failure.Forbidden(forbiddenMessage);

        return topic;
    }
}
