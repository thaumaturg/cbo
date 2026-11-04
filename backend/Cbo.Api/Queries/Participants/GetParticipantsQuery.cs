using Cbo.Api.Contracts.Participants;
using Cbo.Api.Cqrs;
using Cbo.Api.Models.Constants;

namespace Cbo.Api.Queries.Participants;

/// <summary>The tournament's participants, optionally only those with <paramref name="Role"/>, ordered by seed.</summary>
public sealed record GetParticipantsQuery(Guid TournamentId, TournamentParticipantRole? Role) : IQuery<List<ParticipantResponse>>;
