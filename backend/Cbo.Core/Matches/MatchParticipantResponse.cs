using Cbo.Core.Domain;

namespace Cbo.Core.Matches;

public record MatchParticipantResponse
{
    public required Guid Id { get; set; }
    public required Guid TournamentParticipantId { get; set; }
    public required string Username { get; set; }
    public int? ScoreSum { get; set; }
    public int? PositiveScoreSum { get; set; }
    public decimal? PointsSum { get; set; }

    /// <summary>
    /// Where the player of a playoff match, shootout or Final comes from, such as "Q4", "1st of #2" or "dropped from
    /// #8". In a shootout, where they came from into the match it settles. Null in qualification matches.
    /// </summary>
    public string? SourceLabel { get; set; }

    /// <summary>
    /// What the player got from a Decided playoff match or Final; in a shootout, from the match it settles. Null until
    /// that match is Decided.
    /// </summary>
    public Outcome? Outcome { get; set; }

    /// <summary>The player lost a match of the Losers bracket, so is out of the tournament.</summary>
    public bool IsEliminated { get; set; }
}
