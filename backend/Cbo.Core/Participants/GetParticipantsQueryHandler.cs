using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Core.Tournaments;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Participants;

public sealed class GetParticipantsQueryHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService) : IQueryHandler<GetParticipantsQuery, List<ParticipantResponse>>
{
    private const string ReadForbiddenMessage = "You don't have permission to view this tournament.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;

    public async Task<Result<List<ParticipantResponse>>> HandleAsync(GetParticipantsQuery query, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            query.TournamentId, TournamentOperations.Read, ReadForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        IQueryable<TournamentParticipant> participants = _dbContext.TournamentParticipants
            .Where(tp => tp.TournamentId == query.TournamentId);

        if (query.Role.HasValue)
            participants = participants.Where(tp => tp.Role == query.Role.Value);

        // Staff come after the players, in the order the query gives them
        List<ParticipantResponse> responses = await participants
            .OrderBy(tp => tp.Id)
            .Select(ParticipantProjections.ToResponse())
            .ToListAsync(cancellationToken);

        // Before any match is scored every player ties on zero, so players stay in seed order
        IEnumerable<ParticipantResponse> playersInPlaceOrder = QualificationPlaces.InPlaceOrder(
            responses.Where(p => p.Role == TournamentParticipantRole.Player),
            p => new QualificationResult(p.PointsSum ?? 0m, p.ScoreSum ?? 0, p.PositiveScoreSum ?? 0, p.Seed!.Value));

        return playersInPlaceOrder
            .Concat(responses.Where(p => p.Role != TournamentParticipantRole.Player))
            .ToList();
    }
}
