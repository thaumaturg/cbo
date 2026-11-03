using Cbo.API.Contracts.Participants;
using Cbo.API.Cqrs;

namespace Cbo.API.Queries.Participants;

public sealed record GetParticipantQuery(Guid TournamentId, Guid ParticipantId) : IQuery<ParticipantResponse>;
