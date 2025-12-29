using Cbo.API.Models.Constants;

namespace Cbo.API.Models.DTO;

public record GetMatchDto
{
    public required Guid Id { get; set; }
    public required int NumberInTournament { get; set; }
    public required int NumberInStage { get; set; }
    public required TournamentStage CreatedOnStage { get; set; }
    public required Constants.MatchType Type { get; set; }
    public required Guid TournamentId { get; set; }
    public required List<GetMatchParticipantDto> MatchParticipants { get; set; }
    public required int RoundsCount { get; set; }
}
