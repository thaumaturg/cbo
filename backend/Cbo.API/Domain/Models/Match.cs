using Cbo.API.Domain.Constants;

namespace Cbo.API.Domain.Models;

public class Match
{
    public int Id { get; set; }
    public int MatchInSeries { get; set; }
    public TournamentStage CreatedOnStage { get; set; }
    public Constants.MatchType Type { get; set; }
    public bool IsFinished { get; set; }
    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; }


    public ICollection<Round> Rounds { get; set; }
    public ICollection<MatchParticipant> MatchParticipants { get; set; }
    public ICollection<MatchParticipant> SourceForMatchParticipants { get; set; }
} 
