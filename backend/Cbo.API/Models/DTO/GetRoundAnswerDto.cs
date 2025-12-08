using Cbo.API.Models.Domain;

namespace Cbo.API.Models.DTO;

public record GetRoundAnswerDto
{
    public required int Id { get; set; }
    public required bool IsAnswerAccepted { get; set; }
    public required DateTime AnsweredAt { get; set; }
    public Round? Round { get; set; }
    public Question? Question { get; set; }
    public MatchParticipant? MatchParticipant { get; set; }
}
