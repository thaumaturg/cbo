using Cbo.API.Models.Constants;

namespace Cbo.API.Models.DTO;

public class StartTournamentRequest
{
    public required TournamentStage Stage { get; set; }
}
