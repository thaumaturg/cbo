using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Core.Matches;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Playoffs;

public interface IPlayoffProgressionService
{
    /// <summary>
    /// Goes on from a confirmed playoff match, shootout or Final: settles the match it decides (itself, or the match
    /// a shootout settles) from its scores and those of its shootouts. Creates the next shootout while players are
    /// still tied; otherwise writes the match's Outcomes and creates the next matches, see
    /// <see cref="CreateNextMatchesAsync"/>. Stages changes only.
    /// </summary>
    /// <param name="confirmed">Tracked, with its participants.</param>
    Task GoOnFromAsync(Tournament tournament, Match confirmed, CancellationToken cancellationToken);

    /// <summary>
    /// Creates every playoff match and Final that doesn't exist yet and whose players are all known, from the fixed
    /// qualification places and the Outcomes of Decided matches. Reads the tracked graph, so it sees places and
    /// Outcomes that are only staged. Stages changes only.
    /// </summary>
    Task CreateNextMatchesAsync(Tournament tournament, CancellationToken cancellationToken);
}

public class PlayoffProgressionService(CboDbContext dbContext) : IPlayoffProgressionService
{
    private readonly CboDbContext _dbContext = dbContext;

    public async Task GoOnFromAsync(Tournament tournament, Match confirmed, CancellationToken cancellationToken)
    {
        Match settled = confirmed.SettledMatchId is Guid settledMatchId
            ? await _dbContext.Matches
                .Include(m => m.MatchParticipants)
                .SingleAsync(m => m.Id == settledMatchId, cancellationToken)
            : confirmed;

        List<Match> shootouts = await _dbContext.Matches
            .Include(m => m.MatchParticipants)
            .Where(m => m.SettledMatchId == settled.Id)
            .OrderBy(m => m.ShootoutNumber)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, int> places = await QualificationPlacesAsync(tournament, cancellationToken);

        Dictionary<Guid, AnswerScores> matchScores = ScoresOf(settled);
        List<IReadOnlyDictionary<Guid, AnswerScores>> shootoutScores = [.. shootouts.Select(ScoresOf)];
        Settlement<Guid> settlement = settled.Type == MatchType.Final
            ? FinalSettlement.Settle(matchScores, shootoutScores, player => places[player])
            : PlayoffMatchSettlement.Settle(matchScores, shootoutScores, player => places[player]);

        switch (settlement)
        {
            case ShootoutNeeded<Guid> shootout:
                await CreateShootoutAsync(tournament, settled, shootouts.Count + 1, shootout.Players, cancellationToken);
                break;

            case Decided<Guid> decided:
                foreach (MatchParticipant participant in settled.MatchParticipants)
                    participant.Outcome = decided.Outcomes[participant.TournamentParticipantId];

                await CreateNextMatchesAsync(tournament, cancellationToken);
                break;
        }
    }

    public async Task CreateNextMatchesAsync(Tournament tournament, CancellationToken cancellationToken)
    {
        PlayoffBracket bracket = BracketOf(tournament);

        Dictionary<Guid, int> places = await QualificationPlacesAsync(tournament, cancellationToken);
        List<Guid> playersInPlaceOrder = [.. places.OrderBy(entry => entry.Value).Select(entry => entry.Key)];

        List<Match> playoffMatchesAndFinal = await _dbContext.Matches
            .Include(m => m.MatchParticipants)
            .Where(m => m.TournamentId == tournament.Id && m.BracketPosition != null)
            .ToListAsync(cancellationToken);

        Dictionary<BracketPosition, IReadOnlyDictionary<Guid, Outcome>> decided = playoffMatchesAndFinal
            .Where(m => m.MatchParticipants.All(mp => mp.Outcome is not null))
            .ToDictionary(
                m => m.BracketPosition!.Value,
                m => (IReadOnlyDictionary<Guid, Outcome>)m.MatchParticipants.ToDictionary(mp => mp.TournamentParticipantId, mp => mp.Outcome!.Value));

        List<BracketPosition> existing = [.. playoffMatchesAndFinal.Select(m => m.BracketPosition!.Value)];

        int qualificationMatches = MatchGenerationService.QualificationMatchCount(tournament.Format);
        foreach (NextMatch<Guid> next in bracket.NextMatches(playersInPlaceOrder, decided, existing))
        {
            int numberInStage = (int)next.Position;
            Match match = NewMatch(
                tournament,
                next.Position == BracketPosition.Final ? MatchType.Final : MatchType.Playoff,
                qualificationMatches + numberInStage,
                numberInStage,
                next.Players);
            match.BracketPosition = next.Position;
            _dbContext.Matches.Add(match);
        }
    }

    /// <summary>
    /// Adds the match's next shootout. Shootouts are numbered in the tournament after every playoff match and the
    /// Final, in the order they are created.
    /// </summary>
    private async Task CreateShootoutAsync(
        Tournament tournament, Match settled, int shootoutNumber, IEnumerable<Guid> players, CancellationToken cancellationToken)
    {
        int lastNumberInStage = await _dbContext.Matches
            .Where(m => m.TournamentId == tournament.Id && m.CreatedOnStage == TournamentStage.Playoff)
            .MaxAsync(m => (int?)m.NumberInStage, cancellationToken) ?? 0;
        int numberInStage = Math.Max(lastNumberInStage, BracketOf(tournament).Matches.Count) + 1;

        Match shootout = NewMatch(
            tournament,
            MatchType.Shootout,
            MatchGenerationService.QualificationMatchCount(tournament.Format) + numberInStage,
            numberInStage,
            players);
        shootout.SettledMatchId = settled.Id;
        shootout.ShootoutNumber = shootoutNumber;
        _dbContext.Matches.Add(shootout);
    }

    /// <summary>Each player's fixed qualification place, by tournament participant id.</summary>
    private async Task<Dictionary<Guid, int>> QualificationPlacesAsync(Tournament tournament, CancellationToken cancellationToken)
    {
        // Already tracked players come back as they are, so places set by Start Playoff but not saved yet count
        List<TournamentParticipant> players = await _dbContext.TournamentParticipants
            .Where(tp => tp.TournamentId == tournament.Id && tp.Role == TournamentParticipantRole.Player)
            .ToListAsync(cancellationToken);

        return players.ToDictionary(
            tp => tp.Id,
            tp => tp.QualificationPlace
                ?? throw new InvalidOperationException($"Player {tp.Id} has no qualification place, so the Playoff hasn't started."));
    }

    private static Dictionary<Guid, AnswerScores> ScoresOf(Match match) =>
        match.MatchParticipants.ToDictionary(
            mp => mp.TournamentParticipantId,
            mp => new AnswerScores(mp.ScoreSum ?? 0, mp.PositiveScoreSum ?? 0));

    private static PlayoffBracket BracketOf(Tournament tournament) =>
        PlayoffBracket.For(tournament.Format)
            ?? throw new InvalidOperationException($"Format {tournament.Format} has no playoff bracket.");

    private static Match NewMatch(
        Tournament tournament, MatchType type, int numberInTournament, int numberInStage, IEnumerable<Guid> players)
    {
        var match = new Match
        {
            NumberInTournament = numberInTournament,
            NumberInStage = numberInStage,
            CreatedOnStage = TournamentStage.Playoff,
            Type = type,
            TournamentId = tournament.Id,
        };

        foreach (Guid player in players)
            match.MatchParticipants.Add(new MatchParticipant { TournamentParticipantId = player, MatchId = Guid.Empty });

        return match;
    }
}
