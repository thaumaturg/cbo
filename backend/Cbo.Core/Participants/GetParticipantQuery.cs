using Cbo.Core.Cqrs;

namespace Cbo.Core.Participants;

public sealed record GetParticipantQuery(Guid TournamentId, Guid ParticipantId) : IQuery<ParticipantResponse>;
