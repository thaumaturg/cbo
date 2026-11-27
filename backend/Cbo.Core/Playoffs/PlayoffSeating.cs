using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Core.Matches;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Playoffs;

/// <summary>
/// Where each player of a tournament's playoff matches, shootouts and Final comes from, what they got from it and
/// who is Eliminated, as the bracket template gives them from the fixed qualification places and the Outcomes of
/// Decided matches.
/// </summary>
internal sealed class PlayoffSeating
{
    private static readonly PlayoffSeating s_empty = new([]);

    private readonly Dictionary<Guid, SeatedMatch> _byMatch;

    private PlayoffSeating(Dictionary<Guid, SeatedMatch> byMatch)
    {
        _byMatch = byMatch;
    }

    public static async Task<PlayoffSeating> LoadAsync(
        CboDbContext dbContext, Tournament tournament, CancellationToken cancellationToken)
    {
        PlayoffBracket? bracket = PlayoffBracket.For(tournament.Format);
        if (bracket is null)
            return s_empty;

        List<Guid> playersInPlaceOrder = await dbContext.TournamentParticipants
            .Where(tp => tp.TournamentId == tournament.Id && tp.QualificationPlace != null)
            .OrderBy(tp => tp.QualificationPlace)
            .Select(tp => tp.Id)
            .ToListAsync(cancellationToken);
        if (playersInPlaceOrder.Count == 0)
            return s_empty;

        var playoffMatchesAndFinal = await dbContext.Matches
            .Where(m => m.TournamentId == tournament.Id && m.BracketPosition != null)
            .Select(m => new
            {
                m.Id,
                Position = m.BracketPosition!.Value,
                Outcomes = m.MatchParticipants.Select(mp => new { mp.TournamentParticipantId, mp.Outcome }).ToList(),
            })
            .ToListAsync(cancellationToken);

        Dictionary<BracketPosition, IReadOnlyDictionary<Guid, Outcome>> decided = playoffMatchesAndFinal
            .Where(m => m.Outcomes.All(o => o.Outcome is not null))
            .ToDictionary(
                m => m.Position,
                m => (IReadOnlyDictionary<Guid, Outcome>)m.Outcomes.ToDictionary(o => o.TournamentParticipantId, o => o.Outcome!.Value));

        Dictionary<Guid, SeatedMatch> byMatch = [];
        foreach (var match in playoffMatchesAndFinal)
        {
            Dictionary<Guid, string> labels = bracket.PlayersOf(match.Position, playersInPlaceOrder, decided) is { } seats
                ? seats.ToDictionary(seat => seat.Player, seat => seat.Slot.Label)
                : [];

            byMatch[match.Id] = new SeatedMatch(
                labels,
                decided.GetValueOrDefault(match.Position),
                LosersAreEliminated: bracket.Destinations(match.Position).Losers is null);
        }

        return new PlayoffSeating(byMatch);
    }

    /// <summary>
    /// Sets each participant's source label, Outcome and whether they are Eliminated. A shootout's players get those
    /// of the match it settles, so their Outcome shows once that match is Decided.
    /// </summary>
    public void Describe(Guid matchId, Guid? settledMatchId, IEnumerable<MatchParticipantResponse> participants)
    {
        if (!_byMatch.TryGetValue(settledMatchId ?? matchId, out SeatedMatch? seated))
            return;

        foreach (MatchParticipantResponse participant in participants)
        {
            Guid player = participant.TournamentParticipantId;
            participant.SourceLabel = seated.Labels.GetValueOrDefault(player);
            participant.Outcome = seated.Outcomes is not null && seated.Outcomes.TryGetValue(player, out Outcome outcome) ? outcome : null;
            participant.IsEliminated = seated.LosersAreEliminated && participant.Outcome == Outcome.Loser;
        }
    }

    /// <param name="Outcomes">Null until the match is Decided.</param>
    private sealed record SeatedMatch(
        Dictionary<Guid, string> Labels,
        IReadOnlyDictionary<Guid, Outcome>? Outcomes,
        bool LosersAreEliminated);
}
