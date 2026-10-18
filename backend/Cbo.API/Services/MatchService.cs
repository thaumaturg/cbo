using Cbo.API.Authorization;
using Cbo.API.Mappings;
using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Repositories;
using Cbo.Results;

namespace Cbo.API.Services;

public interface IMatchService
{
    Task<Result<List<GetMatchDto>>> GetMatchesAsync(Guid tournamentId);
    Task<Result<GetMatchDetailDto>> GetMatchAsync(Guid tournamentId, Guid matchId);
    Task<Result<List<GetAvailableTopicDto>>> GetAvailableTopicsAsync(Guid tournamentId, Guid matchId);
}

public class MatchService(
    ITournamentAccessService tournamentAccessService,
    IMatchRepository matchRepository,
    IRoundRepository roundRepository,
    ITournamentTopicRepository tournamentTopicRepository) : IMatchService
{
    private const string ReadForbiddenMessage = "You don't have permission to view this tournament.";
    private const string ViewAllTopicsForbiddenMessage = "You don't have permission to view all topics of this tournament.";

    private readonly ITournamentAccessService _tournamentAccessService = tournamentAccessService;
    private readonly IMatchRepository _matchRepository = matchRepository;
    private readonly IRoundRepository _roundRepository = roundRepository;
    private readonly ITournamentTopicRepository _tournamentTopicRepository = tournamentTopicRepository;

    public async Task<Result<List<GetMatchDto>>> GetMatchesAsync(Guid tournamentId)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(tournamentId, TournamentOperations.Read, ReadForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        List<Match> matches = await _matchRepository.GetAllByTournamentIdAsync(tournamentId);

        return matches.Select(m => m.ToGetDto()).ToList();
    }

    public async Task<Result<GetMatchDetailDto>> GetMatchAsync(Guid tournamentId, Guid matchId)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(tournamentId, TournamentOperations.Read, ReadForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        Match? match = await _matchRepository.GetByIdWithDetailsAsync(matchId);
        if (match is null || match.TournamentId != tournamentId)
            return MatchNotFound();

        match.Rounds = match.Rounds.OrderBy(r => r.NumberInMatch).ToList();

        List<TournamentTopic> tournamentTopics = await _tournamentTopicRepository.GetAllByTournamentIdAsync(tournamentId);
        Dictionary<Guid, RoundTopicInfo> topicInfoByTopicId = tournamentTopics.ToDictionary(
            tt => tt.TopicId,
            tt => new RoundTopicInfo(tt.PriorityIndex, tt.TournamentParticipant.ApplicationUser?.UserName ?? string.Empty));

        return match.ToDetailDto(topicInfoByTopicId);
    }

    public async Task<Result<List<GetAvailableTopicDto>>> GetAvailableTopicsAsync(Guid tournamentId, Guid matchId)
    {
        Result<Tournament> access = await _tournamentAccessService.LoadTournamentWithAccessAsync(tournamentId, TournamentOperations.ViewAllTopics, ViewAllTopicsForbiddenMessage);
        if (access.IsFailure)
            return access.Failure;

        Match? match = await _matchRepository.GetByIdWithParticipantsAsync(matchId);
        if (match is null || match.TournamentId != tournamentId)
            return MatchNotFound();

        HashSet<Guid> participantUserIds = match.MatchParticipants
            .Select(mp => mp.TournamentParticipant.ApplicationUserId)
            .ToHashSet();

        List<TournamentTopic> tournamentTopics = await _tournamentTopicRepository.GetAllByTournamentIdWithAuthorsAsync(tournamentId);

        List<Round> tournamentRounds = await _roundRepository.GetAllByTournamentIdAsync(tournamentId);
        HashSet<Guid> playedTopicIds = tournamentRounds.Select(r => r.TopicId).ToHashSet();

        // A topic is available when it hasn't been played yet and none of the match's players is one of its authors
        return tournamentTopics
            .Where(tt => !playedTopicIds.Contains(tt.TopicId))
            .Where(tt => tt.Topic?.TopicAuthors is null ||
                         !tt.Topic.TopicAuthors.Any(ta => ta.IsAuthor && participantUserIds.Contains(ta.ApplicationUserId)))
            .OrderBy(tt => tt.PriorityIndex)
            .Select(tt => tt.ToAvailableDto())
            .ToList();
    }

    private static Failure MatchNotFound() =>
        Failure.NotFound("Match not found in this tournament.");
}
