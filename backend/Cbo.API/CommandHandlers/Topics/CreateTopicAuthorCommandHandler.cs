using Cbo.API.Authorization;
using Cbo.API.Commands.Topics;
using Cbo.API.Cqrs;
using Cbo.API.Data;
using Cbo.API.Data.Configuration;
using Cbo.API.Models.Domain;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.CommandHandlers.Topics;

/// <summary>
/// Adds a user as a co-author of the topic. Co-authors aren't owners.
/// </summary>
public sealed class CreateTopicAuthorCommandHandler(
    CboDbContext dbContext,
    ITopicAccessService topicAccessService,
    UserManager<ApplicationUser> userManager) : CommandHandler<CreateTopicAuthorCommand, Guid>(dbContext)
{
    private const string ManageAuthorsForbiddenMessage = "You don't have permission to manage the authors of this topic.";

    private readonly ITopicAccessService _topicAccessService = topicAccessService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    protected override async Task<Result<Guid>> ExecuteAsync(CreateTopicAuthorCommand command, CancellationToken cancellationToken)
    {
        Result<Topic> access = await _topicAccessService.LoadTopicWithAccessAsync(
            command.TopicId, TopicOperations.ManageAuthors, ManageAuthorsForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        ApplicationUser? user = await _userManager.FindByNameAsync(command.Username);
        if (user is null)
            return Failure.Invalid(
                "Some of the author data is invalid.",
                new FieldError("username", $"User '{command.Username}' doesn't exist."));

        if (await TopicRules.IsPlayedAsync(DbContext, command.TopicId, cancellationToken))
            return TopicFailures.PlayedAuthorsCannotChange();

        bool isAlreadyAnAuthor = await DbContext.TopicAuthors
            .AnyAsync(ta => ta.ApplicationUserId == user.Id && ta.TopicId == command.TopicId, cancellationToken);
        if (isAlreadyAnAuthor)
            return AlreadyAnAuthor(command.Username);

        // Set here rather than by the database default, so the id is known before the save
        var author = new TopicAuthor
        {
            Id = Guid.CreateVersion7(),
            IsOwner = false,
            IsAuthor = true,
            TopicId = command.TopicId,
            ApplicationUserId = user.Id,
        };

        DbContext.TopicAuthors.Add(author);

        return author.Id;
    }

    protected override Failure? MapSaveFailure(CreateTopicAuthorCommand command, DbUpdateException exception)
    {
        // Lost a race with a simultaneous add of the same author
        if (PostgresConstraint.IsUniqueViolation(exception, TopicAuthorConfiguration.TopicIdApplicationUserIdIndexName))
            return AlreadyAnAuthor(command.Username);

        return null;
    }

    private static Failure AlreadyAnAuthor(string username) =>
        Failure.Conflict($"User '{username}' is already an author of this topic.");
}
