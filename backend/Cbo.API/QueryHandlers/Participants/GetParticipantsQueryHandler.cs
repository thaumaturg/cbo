using Cbo.API.Authorization;
using Cbo.API.Cqrs;
using Cbo.API.Data;
using Cbo.API.Mappings;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Queries.Participants;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.QueryHandlers.Participants;

public sealed class GetParticipantsQueryHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService) : IQueryHandler<GetParticipantsQuery, List<GetTournamentParticipantDto>>
{
    private const string ReadForbiddenMessage = "You don't have permission to view this tournament.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;

    public async Task<Result<List<GetTournamentParticipantDto>>> HandleAsync(GetParticipantsQuery query, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            query.TournamentId, TournamentOperations.Read, ReadForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        IQueryable<TournamentParticipant> participants = _dbContext.TournamentParticipants
            .Where(tp => tp.TournamentId == query.TournamentId);

        if (query.Role.HasValue)
            participants = participants.Where(tp => tp.Role == query.Role.Value);

        // Postgres sorts null seeds (non-players) last
        return await participants
            .OrderBy(tp => tp.Seed)
            .ThenBy(tp => tp.Id)
            .Select(ParticipantProjections.ToGetDto())
            .ToListAsync(cancellationToken);
    }
}
