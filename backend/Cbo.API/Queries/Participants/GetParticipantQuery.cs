using Cbo.API.Cqrs;
using Cbo.API.Models.DTO;

namespace Cbo.API.Queries.Participants;

public sealed record GetParticipantQuery(Guid TournamentId, Guid ParticipantId) : IQuery<GetTournamentParticipantDto>;
