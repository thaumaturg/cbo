using Cbo.Core.Domain;

namespace Cbo.Core.Tournaments;

/// <summary>
/// The query string of <c>GET /Tournaments</c>. Every property is nullable, so a missing parameter gets its default
/// instead of a binding error. <see cref="Role"/> is singular because it repeats once per value.
/// </summary>
public record GetTournamentsRequest
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
    public string? Search { get; set; }
    public TournamentStageFilter? Stage { get; set; }
    public TournamentParticipantRole[]? Role { get; set; }
}
