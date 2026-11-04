using Cbo.Core.Cqrs;
using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Core.Tournaments;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Participants;

public sealed class GetParticipantQueryHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService) : IQueryHandler<GetParticipantQuery, ParticipantResponse>
{
    private const string ReadForbiddenMessage = "You don't have permission to view this tournament.";
    private const string NotFoundMessage = "Participant not found in this tournament.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;

    public async Task<Result<ParticipantResponse>> HandleAsync(GetParticipantQuery query, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            query.TournamentId, TournamentOperations.Read, ReadForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        ParticipantResponse? participant = await _dbContext.TournamentParticipants
            .Where(tp => tp.Id == query.ParticipantId && tp.TournamentId == query.TournamentId)
            .Select(ParticipantProjections.ToResponse())
            .FirstOrDefaultAsync(cancellationToken);
        if (participant is null)
            return Failure.NotFound(NotFoundMessage);

        return participant;
    }
}
