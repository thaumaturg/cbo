using Cbo.API.Authorization;
using Cbo.API.Cqrs;
using Cbo.API.Data;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Queries.TournamentTopics;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.QueryHandlers.TournamentTopics;

public sealed class GetTournamentTopicQueryHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService,
    ICurrentUserService currentUserService) : IQueryHandler<GetTournamentTopicQuery, GetTopicDto>
{
    private const string ViewAllTopicsForbiddenMessage = "You don't have permission to view all topics of this tournament.";
    private const string NotFoundMessage = "Topic is not part of this tournament.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<Result<GetTopicDto>> HandleAsync(GetTournamentTopicQuery query, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            query.TournamentId, TournamentOperations.ViewAllTopics, ViewAllTopicsForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        Guid currentUserId = _currentUserService.GetRequiredCurrentUserId();

        GetTopicDto? topic = await _dbContext.Topics
            .Where(t => t.Id == query.TopicId && t.TournamentTopics.Any(tt => tt.TournamentId == query.TournamentId))
            .Select(t => new GetTopicDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                IsPlayed = t.Rounds.Any(),
                IsAuthor = t.TopicAuthors
                    .Where(ta => ta.ApplicationUserId == currentUserId)
                    .Select(ta => ta.IsAuthor)
                    .FirstOrDefault(),
                Questions = t.Questions
                    .OrderBy(q => q.QuestionNumber)
                    .Select(q => new GetQuestionDto
                    {
                        Id = q.Id,
                        QuestionNumber = q.QuestionNumber,
                        CostPositive = q.CostPositive,
                        CostNegative = q.CostNegative,
                        Text = q.Text,
                        Answer = q.Answer,
                        Comment = q.Comment,
                        TopicId = q.TopicId,
                    })
                    .ToList(),
                Authors = t.TopicAuthors
                    .Select(ta => new GetTopicAuthorDto
                    {
                        Id = ta.Id,
                        Username = ta.ApplicationUser!.UserName ?? string.Empty,
                        IsOwner = ta.IsOwner,
                        IsAuthor = ta.IsAuthor,
                        TopicId = ta.TopicId,
                        ApplicationUserId = ta.ApplicationUserId,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (topic is null)
            return Failure.NotFound(NotFoundMessage);

        return topic;
    }
}
