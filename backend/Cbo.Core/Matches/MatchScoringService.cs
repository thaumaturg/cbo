using Cbo.Core.Data;
using Cbo.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Matches;

public interface IMatchScoringService
{
    /// <summary>
    /// Loads the match tracked, with everything <see cref="RecalculateScoresAsync"/> reads: its participants, and its
    /// rounds with their answers and the answers' questions. Null when the match isn't part of the tournament.
    /// </summary>
    Task<Match?> LoadMatchAsync(Guid tournamentId, Guid matchId, CancellationToken cancellationToken);

    /// <summary>
    /// Recomputes the match's results with <see cref="MatchScores.Recalculate"/>, then the totals of every participant
    /// of the tournament, which count qualification matches only. Reads the
    /// tracked graph rather than the database, so it sees round changes that are only staged: apply them to the graph
    /// from <see cref="LoadMatchAsync"/> first. Every answer in the graph needs its <see cref="RoundAnswer.Question"/>.
    /// Stages changes only.
    /// </summary>
    Task RecalculateScoresAsync(Match match, CancellationToken cancellationToken);
}

public class MatchScoringService(CboDbContext dbContext) : IMatchScoringService
{
    private readonly CboDbContext _dbContext = dbContext;

    public Task<Match?> LoadMatchAsync(Guid tournamentId, Guid matchId, CancellationToken cancellationToken) =>
        _dbContext.Matches
            .Include(m => m.MatchParticipants)
            .Include(m => m.Rounds)
                .ThenInclude(r => r.RoundAnswers)
                    .ThenInclude(ra => ra.Question)
            .FirstOrDefaultAsync(m => m.Id == matchId && m.TournamentId == tournamentId, cancellationToken);

    public async Task RecalculateScoresAsync(Match match, CancellationToken cancellationToken)
    {
        MatchScores.Recalculate(match);
        await RecalculateTournamentScoresAsync(match.TournamentId, cancellationToken);
    }

    private async Task RecalculateTournamentScoresAsync(Guid tournamentId, CancellationToken cancellationToken)
    {
        // The match's participants are already tracked, so the query returns those instances with their new scores
        List<TournamentParticipant> participants = await _dbContext.TournamentParticipants
            .Include(tp => tp.MatchParticipants)
                .ThenInclude(mp => mp.Match)
            .Where(tp => tp.TournamentId == tournamentId)
            .ToListAsync(cancellationToken);

        foreach (TournamentParticipant participant in participants)
        {
            QualificationTotals totals = QualificationTotals.Of(participant.MatchParticipants);

            participant.ScoreSum = totals.Score;
            participant.PositiveScoreSum = totals.PositiveScore;
            participant.PointsSum = totals.Points;
        }
    }
}
