using Cbo.API.Models.Constants;

namespace Cbo.API.Models.Domain;

public class Match
{
    public required int Id { get; set; }
    public required int NumberInTournament { get; set; }
    public required int NumberInStage { get; set; }
    public required TournamentStage CreatedOnStage { get; set; }
    public required Constants.MatchType Type { get; set; }
    public required bool IsFinished { get; set; }
    public required int TournamentId { get; set; }
    public required Tournament Tournament { get; set; }
    public ICollection<Round> Rounds { get; set; } = [];
    public ICollection<MatchParticipant> MatchParticipants { get; set; } = [];
}
