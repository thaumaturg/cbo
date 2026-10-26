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

public sealed class GetParticipantQueryHandler(
    CboDbContext dbContext,
    ITournamentAccessService tournamentAccessService) : IQueryHandler<GetParticipantQuery, GetTournamentParticipantDto>
{
    private const string ReadForbiddenMessage = "You don't have permission to view this tournament.";
    private const string NotFoundMessage = "Participant not found in this tournament.";

    private readonly CboDbContext _dbContext = dbContext;
    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;

    public async Task<Result<GetTournamentParticipantDto>> HandleAsync(GetParticipantQuery query, CancellationToken cancellationToken)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(
            query.TournamentId, TournamentOperations.Read, ReadForbiddenMessage, cancellationToken);
        if (access.IsFailure)
            return access.Failure;

        GetTournamentParticipantDto? participant = await _dbContext.TournamentParticipants
            .Where(tp => tp.Id == query.ParticipantId && tp.TournamentId == query.TournamentId)
            .Select(ParticipantProjections.ToGetDto())
            .FirstOrDefaultAsync(cancellationToken);
        if (participant is null)
            return Failure.NotFound(NotFoundMessage);

        return participant;
    }
}
