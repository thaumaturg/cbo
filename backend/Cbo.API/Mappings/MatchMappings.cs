using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;

namespace Cbo.API.Mappings;

/// <summary>
/// Holds topic info needed for round mapping. Used instead of passing domain models.
/// </summary>
public record RoundTopicInfo(int PriorityIndex, string OwnerUsername);

public static class MatchMappings
{
    /// <summary>
    /// Maps a Match to GetMatchDto.
    /// Requires: MatchParticipants (with TournamentParticipant.ApplicationUser) and Rounds to be loaded.
    /// </summary>
    public static GetMatchDto ToGetDto(this Match match)
    {
        return new GetMatchDto
        {
            Id = match.Id,
            NumberInTournament = match.NumberInTournament,
            NumberInStage = match.NumberInStage,
            CreatedOnStage = match.CreatedOnStage,
            Type = match.Type,
            TournamentId = match.TournamentId,
            MatchParticipants = match.MatchParticipants.Select(mp => mp.ToGetDto()).ToList(),
            RoundsCount = match.Rounds.Count
        };
    }

    /// <summary>
    /// Maps a Match to GetMatchDetailDto with round details.
    /// Requires: MatchParticipants (with TournamentParticipant.ApplicationUser),
    /// Rounds (with Topic, Questions, RoundAnswers) to be loaded.
    /// </summary>
    public static GetMatchDetailDto ToDetailDto(this Match match, IReadOnlyDictionary<Guid, RoundTopicInfo> topicInfoByTopicId)
    {
        return new GetMatchDetailDto
        {
            Id = match.Id,
            NumberInTournament = match.NumberInTournament,
            NumberInStage = match.NumberInStage,
            CreatedOnStage = match.CreatedOnStage,
            Type = match.Type,
            TournamentId = match.TournamentId,
            MatchParticipants = match.MatchParticipants.Select(mp => mp.ToGetDto()).ToList(),
            Rounds = match.Rounds
                .Select(r =>
                {
                    RoundTopicInfo info = topicInfoByTopicId[r.TopicId];
                    return r.ToGetDto(info.PriorityIndex, info.OwnerUsername);
                })
                .ToList()
        };
    }

    /// <summary>
    /// Maps a MatchParticipant to GetMatchParticipantDto.
    /// Requires: TournamentParticipant.ApplicationUser to be loaded (optional, will use empty string if null).
    /// </summary>
    public static GetMatchParticipantDto ToGetDto(this MatchParticipant matchParticipant)
    {
        return new GetMatchParticipantDto
        {
            Id = matchParticipant.Id,
            Username = matchParticipant.TournamentParticipant?.ApplicationUser?.UserName ?? string.Empty,
            ScoreSum = matchParticipant.ScoreSum,
            PointsSum = matchParticipant.PointsSum
        };
    }
}
