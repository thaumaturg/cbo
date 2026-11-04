using Cbo.Core.Cqrs;
using Cbo.Core.Models.Constants;

namespace Cbo.Core.Participants;

/// <summary>The tournament's participants, optionally only those with <paramref name="Role"/>, ordered by seed.</summary>
public sealed record GetParticipantsQuery(Guid TournamentId, TournamentParticipantRole? Role) : IQuery<List<ParticipantResponse>>;
