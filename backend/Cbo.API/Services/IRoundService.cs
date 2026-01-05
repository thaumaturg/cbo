using Cbo.API.Models.DTO;

namespace Cbo.API.Services;

public interface IRoundService
{
    string? ValidateRoundAnswers(List<CreateRoundAnswerDto> answers, bool isOverrideMode);
    Task RecalculateMatchScoresAsync(Guid matchId);
}
