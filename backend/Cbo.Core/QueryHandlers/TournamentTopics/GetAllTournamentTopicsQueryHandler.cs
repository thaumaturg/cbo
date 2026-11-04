using Cbo.Core.Authorization;
using Cbo.Core.Contracts.TournamentTopics;
using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Mappings;
using Cbo.Core.Models.Domain;
using Cbo.Core.Queries.TournamentTopics;
using Cbo.Core.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.QueryHandlers.TournamentTopics;

public sealed class GetAllTournamentTopicsQueryHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService) : IQueryHandler<GetAllTournamentTopicsQuery, List<TournamentTopicResponse>>
{
    private const string ViewAllTopicsForbiddenMessage = "You don't have permission to view all topics of this tournament.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;

    public async Task<Result<List<TournamentTopicResponse>>> HandleAsync(GetAllTournamentTopicsQuery query, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            query.TournamentId, TournamentOperations.ViewAllTopics, ViewAllTopicsForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        return await _dbContext.TournamentTopics
            .Where(tt => tt.TournamentId == query.TournamentId)
            .OrderBy(tt => tt.TournamentParticipantId)
            .ThenBy(tt => tt.PriorityIndex)
            .Select(TournamentTopicProjections.ToResponse())
            .ToListAsync(cancellationToken);
    }
}
