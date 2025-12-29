using Cbo.API.Models.Domain;
using Cbo.API.Models.DTO;
using Cbo.API.Repositories;

namespace Cbo.API.Services;

public class RoundService : IRoundService
{
    private readonly IMatchRepository _matchRepository;
    private readonly ITournamentParticipantsRepository _participantsRepository;

    public RoundService(IMatchRepository matchRepository, ITournamentParticipantsRepository participantsRepository)
    {
        _matchRepository = matchRepository;
        _participantsRepository = participantsRepository;
    }

    public string? ValidateRoundAnswers(List<CreateRoundAnswerDto> answers)
    {
        var positiveAnswersByQuestion = answers
            .Where(a => a.IsAnswerAccepted)
            .GroupBy(a => a.QuestionId)
            .Where(g => g.Count() > 1)
            .ToList();

        if (positiveAnswersByQuestion.Count > 0)
        {
            Guid questionId = positiveAnswersByQuestion.First().Key;
            return $"Question {questionId} has multiple correct answers. Only one correct answer is allowed per question.";
        }

        return null;
    }

    public async Task RecalculateMatchScoresAsync(Guid matchId)
    {
        Match? match = await _matchRepository.GetByIdWithScoreDataAsync(matchId);
        if (match is null)
            return;

        foreach (MatchParticipant participant in match.MatchParticipants)
        {
            int score = 0;
            foreach (RoundAnswer answer in participant.RoundAnswers)
            {
                if (answer.IsAnswerAccepted)
                    score += answer.Question.CostPositive;
                else
                    score -= answer.Question.CostNegative;
            }
            participant.ScoreSum = score;
        }

        if (match.Rounds.Count == 4)
        {
            CalculatePoints(match.MatchParticipants.ToList());
        }
        else
        {
            foreach (MatchParticipant participant in match.MatchParticipants)
            {
                participant.PointsSum = null;
            }
        }

        await _matchRepository.UpdateMatchParticipantsAsync(match.MatchParticipants.ToList());

        await RecalculateTournamentScoresAsync(match.TournamentId);
    }

    private async Task RecalculateTournamentScoresAsync(Guid tournamentId)
    {
        List<TournamentParticipant> participants = await _participantsRepository
            .GetAllByTournamentIdWithMatchDataAsync(tournamentId);

        foreach (TournamentParticipant participant in participants)
        {
            int scoreSum = participant.MatchParticipants
                .Where(mp => mp.ScoreSum.HasValue)
                .Sum(mp => mp.ScoreSum!.Value);

            decimal pointsSum = participant.MatchParticipants
                .Where(mp => mp.PointsSum.HasValue)
                .Sum(mp => mp.PointsSum!.Value);

            participant.ScoreSum = scoreSum;
            participant.PointsSum = pointsSum;
        }

        await _participantsRepository.UpdateParticipantsAsync(participants);
    }

    private static void CalculatePoints(List<MatchParticipant> participants)
    {
        int count = participants.Count;
        if (count < 2 || count > 4)
            return;

        var sorted = participants.OrderByDescending(p => p.ScoreSum ?? 0).ToList();
        decimal[] points = GetPointsDistribution(sorted);

        for (int i = 0; i < sorted.Count; i++)
        {
            sorted[i].PointsSum = points[i];
        }
    }

    private static decimal[] GetPointsDistribution(List<MatchParticipant> sorted)
    {
        int count = sorted.Count;
        int[] scores = sorted.Select(p => p.ScoreSum ?? 0).ToArray();

        return count switch
        {
            2 => GetPointsFor2Players(scores),
            3 => GetPointsFor3Players(scores),
            4 => GetPointsFor4Players(scores),
            _ => []
        };
    }

    private static decimal[] GetPointsFor2Players(int[] scores)
    {
        if (scores[0] == scores[1])
            return [3m, 3m];

        return [4m, 2m];
    }

    private static decimal[] GetPointsFor3Players(int[] scores)
    {
        bool top2Same = scores[0] == scores[1];
        bool bottom2Same = scores[1] == scores[2];

        if (top2Same && bottom2Same)
            return [2m, 2m, 2m];

        if (top2Same)
            return [2.5m, 2.5m, 1m];

        if (bottom2Same)
            return [3m, 1.5m, 1.5m];

        return [3m, 2m, 1m];
    }

    private static decimal[] GetPointsFor4Players(int[] scores)
    {
        bool s01 = scores[0] == scores[1];
        bool s12 = scores[1] == scores[2];
        bool s23 = scores[2] == scores[3];

        if (s01 && s12 && s23)
            return [1.5m, 1.5m, 1.5m, 1.5m];

        if (s01 && s12 && !s23)
            return [2m, 2m, 2m, 0m];

        if (!s01 && s12 && s23)
            return [3m, 1m, 1m, 1m];

        if (s01 && !s12 && s23)
            return [2.5m, 2.5m, 0.5m, 0.5m];

        if (s01 && !s12 && !s23)
            return [2.5m, 2.5m, 1m, 0m];

        if (!s01 && s12 && !s23)
            return [3m, 1.5m, 1.5m, 0m];

        if (!s01 && !s12 && s23)
            return [3m, 2m, 0.5m, 0.5m];

        return [3m, 2m, 1m, 0m];
    }
}
