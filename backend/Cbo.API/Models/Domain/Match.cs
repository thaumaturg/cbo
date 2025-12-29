using Cbo.API.Models.Constants;

namespace Cbo.API.Models.Domain;

public class Match
{
    public Guid Id { get; set; }
    public required int NumberInTournament { get; set; }
    public required int NumberInStage { get; set; }
    public required TournamentStage CreatedOnStage { get; set; }
    public required Constants.MatchType Type { get; set; }
    public Guid TournamentId { get; set; }
    public required Tournament Tournament { get; set; }
    public ICollection<Round> Rounds { get; set; } = [];
    public ICollection<MatchParticipant> MatchParticipants { get; set; } = [];
}
