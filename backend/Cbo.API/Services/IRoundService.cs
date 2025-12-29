using Cbo.API.Models.DTO;

namespace Cbo.API.Services;

public interface IRoundService
{
    string? ValidateRoundAnswers(List<CreateRoundAnswerDto> answers);
    Task RecalculateMatchScoresAsync(Guid matchId);
}
