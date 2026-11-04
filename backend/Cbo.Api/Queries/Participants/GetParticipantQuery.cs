using Cbo.Api.Contracts.Participants;
using Cbo.Api.Cqrs;

namespace Cbo.Api.Queries.Participants;

public sealed record GetParticipantQuery(Guid TournamentId, Guid ParticipantId) : IQuery<ParticipantResponse>;
