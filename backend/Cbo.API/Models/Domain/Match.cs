using Cbo.API.Models.Constants;

namespace Cbo.API.Models.Domain;

public class Match
{
    public int Id { get; set; }
    public int NumberInTournament { get; set; }
    public int NumberInStage { get; set; }
    public TournamentStage CreatedOnStage { get; set; }
    public Constants.MatchType Type { get; set; }
    public bool IsFinished { get; set; }
    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; }
    public ICollection<Round> Rounds { get; set; }
    public ICollection<MatchParticipant> MatchParticipants { get; set; }
}
