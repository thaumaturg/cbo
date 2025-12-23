using Cbo.API.Models.Constants;

namespace Cbo.API.Models.DTO;

public record GetMatchDto
{
    public required int Id { get; set; }
    public required int NumberInTournament { get; set; }
    public required int NumberInStage { get; set; }
    public required TournamentStage CreatedOnStage { get; set; }
    public required Constants.MatchType Type { get; set; }
    public required int TournamentId { get; set; }
}
