using Cbo.API.Models.Domain;

namespace Cbo.API.Services;

public interface IMatchGenerationService
{
    List<Match> GenerateQualificationMatches(Tournament tournament, List<TournamentParticipant> players);
}
