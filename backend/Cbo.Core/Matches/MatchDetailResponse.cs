using Cbo.Core.Models.Constants;
using Cbo.Core.Rounds;

namespace Cbo.Core.Matches;

public record MatchDetailResponse
{
    public required Guid Id { get; set; }
    public required int NumberInTournament { get; set; }
    public required int NumberInStage { get; set; }
    public required TournamentStage CreatedOnStage { get; set; }
    public required Models.Constants.MatchType Type { get; set; }
    public required Guid TournamentId { get; set; }
    public required List<MatchParticipantResponse> MatchParticipants { get; set; }
    public required List<RoundResponse> Rounds { get; set; }
}
