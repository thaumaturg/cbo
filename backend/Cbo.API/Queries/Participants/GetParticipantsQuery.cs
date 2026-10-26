using Cbo.API.Cqrs;
using Cbo.API.Models.Constants;
using Cbo.API.Models.DTO;

namespace Cbo.API.Queries.Participants;

/// <summary>The tournament's participants, optionally only those with <paramref name="Role"/>, ordered by seed.</summary>
public sealed record GetParticipantsQuery(Guid TournamentId, TournamentParticipantRole? Role) : IQuery<List<GetTournamentParticipantDto>>;
