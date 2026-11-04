using Cbo.Core.Contracts.Participants;
using Cbo.Core.Cqrs;

namespace Cbo.Core.Queries.Participants;

public sealed record GetParticipantQuery(Guid TournamentId, Guid ParticipantId) : IQuery<ParticipantResponse>;
