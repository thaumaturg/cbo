using System.Collections.Immutable;
using Cbo.Core.Domain;

namespace Cbo.Core.Playoffs;

/// <summary>A playoff match or the Final that can now be created, with its players in seat order.</summary>
public sealed record NextMatch<TPlayer>(BracketPosition Position, ImmutableArray<TPlayer> Players);

/// <summary>A player of a playoff match or the Final and the seat they took in it.</summary>
public sealed record SeatedPlayer<TPlayer>(TPlayer Player, BracketSlot Slot);

/// <summary>Where a match sends its winners, and its losers; losers sent nowhere are Eliminated.</summary>
public sealed record BracketDestinations(ImmutableArray<BracketPosition> Winners, BracketPosition? Losers);

/// <summary>
/// What reopening a match deletes: the match's later shootouts and the existing matches after it in the bracket.
/// Deleting a match deletes its own shootouts too.
/// </summary>
public sealed record ReopenDeletion(ImmutableArray<int> Shootouts, ImmutableArray<BracketPosition> Positions);

/// <summary>
/// A format's playoff bracket template: the seats of each playoff match and the Final, and so how they connect.
/// Nothing about the connections is stored; everything here follows from the template and the Outcomes of Decided
/// matches, see docs/adr/0012-playoff-results-are-confirmed-and-corrected-by-reopening.md.
/// </summary>
public sealed class PlayoffBracket
{
    private static readonly PlayoffBracket s_twelve = new(new Dictionary<BracketPosition, BracketSlot[]>
    {
        [BracketPosition.Playoff1] = [Q(5), Q(7), Q(10), Q(12)],
        [BracketPosition.Playoff2] = [Q(6), Q(8), Q(9), Q(11)],
        [BracketPosition.Playoff3] = [Q(1), Q(4), First(BracketPosition.Playoff1), First(BracketPosition.Playoff2)],
        [BracketPosition.Playoff4] = [Q(2), Q(3), Second(BracketPosition.Playoff1), Second(BracketPosition.Playoff2)],
        [BracketPosition.Playoff5] = [.. Losers(BracketPosition.Playoff1), .. Losers(BracketPosition.Playoff2)],
        [BracketPosition.Playoff6] = [.. Losers(BracketPosition.Playoff3), .. Losers(BracketPosition.Playoff4)],
        [BracketPosition.Playoff7] = [.. Winners(BracketPosition.Playoff5), .. Winners(BracketPosition.Playoff6)],
        [BracketPosition.Playoff8] = [.. Winners(BracketPosition.Playoff3), .. Winners(BracketPosition.Playoff4)],
        [BracketPosition.Playoff9] = [.. Losers(BracketPosition.Playoff8), .. Winners(BracketPosition.Playoff7)],
        [BracketPosition.Final] = [.. Winners(BracketPosition.Playoff8), .. Winners(BracketPosition.Playoff9)],
    });

    private PlayoffBracket(Dictionary<BracketPosition, BracketSlot[]> matches)
    {
        Matches = matches.ToImmutableSortedDictionary(
            entry => entry.Key,
            entry => ImmutableArray.Create(entry.Value));
    }

    /// <summary>The seats of each playoff match and the Final, in seat order.</summary>
    public ImmutableSortedDictionary<BracketPosition, ImmutableArray<BracketSlot>> Matches { get; }

    /// <summary>The format's playoff bracket, or null when the format has none yet.</summary>
    public static PlayoffBracket? For(TournamentFormat format) => format switch
    {
        TournamentFormat.TwelveShort or TournamentFormat.TwelveLong => s_twelve,
        TournamentFormat.Fourteen => null,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown tournament format."),
    };

    /// <summary>Where the match sends its winners and its losers.</summary>
    public BracketDestinations Destinations(BracketPosition position) =>
        new(
            Winners: [.. Taking(position, Outcome.FirstWinner, Outcome.SecondWinner)],
            Losers: Taking(position, Outcome.Loser).Cast<BracketPosition?>().SingleOrDefault());

    /// <summary>
    /// The players of a playoff match or the Final, each with the seat they take, or null while a match it takes
    /// players from isn't Decided. Players with the same seat, the losers of one match, sit in qualification place
    /// order.
    /// </summary>
    /// <param name="playersInPlaceOrder">Every player, from the 1st qualification place down.</param>
    /// <param name="decided">The Outcomes of every Decided playoff match and the Final.</param>
    public ImmutableArray<SeatedPlayer<TPlayer>>? PlayersOf<TPlayer>(
        BracketPosition position,
        IReadOnlyList<TPlayer> playersInPlaceOrder,
        IReadOnlyDictionary<BracketPosition, IReadOnlyDictionary<TPlayer, Outcome>> decided)
        where TPlayer : notnull
    {
        Dictionary<TPlayer, int> places = playersInPlaceOrder
            .Select((player, index) => (Player: player, Place: index + 1))
            .ToDictionary(entry => entry.Player, entry => entry.Place);

        ImmutableArray<SeatedPlayer<TPlayer>>.Builder seated = ImmutableArray.CreateBuilder<SeatedPlayer<TPlayer>>();

        foreach (IGrouping<BracketSlot, BracketSlot> seats in Matches[position].GroupBy(slot => slot))
        {
            List<TPlayer>? players = PlayersFor(seats.Key);
            if (players is null)
                return null;

            if (players.Count != seats.Count())
            {
                throw new ArgumentException(
                    $"{seats.Key.Label} has {players.Count} players for {seats.Count()} seats in #{(int)position}.",
                    nameof(decided));
            }

            seated.AddRange(players.Select(player => new SeatedPlayer<TPlayer>(player, seats.Key)));
        }

        return seated.ToImmutable();

        List<TPlayer>? PlayersFor(BracketSlot slot) => slot switch
        {
            QualificationPlaceSlot qualification => [playersInPlaceOrder[qualification.Place - 1]],
            OutcomeSlot outcome when decided.TryGetValue(outcome.Source, out IReadOnlyDictionary<TPlayer, Outcome>? outcomes) =>
                [.. outcomes.Where(entry => entry.Value == outcome.Outcome).Select(entry => entry.Key).OrderBy(player => places[player])],
            OutcomeSlot => null,
            _ => throw new InvalidOperationException($"Unknown bracket slot {slot}."),
        };
    }

    /// <summary>
    /// The playoff matches and Final that don't exist yet and whose players are all known now, in bracket order. With nothing
    /// Decided and nothing existing, these are the Playoff's first matches.
    /// </summary>
    /// <param name="playersInPlaceOrder">Every player, from the 1st qualification place down.</param>
    /// <param name="decided">The Outcomes of every Decided playoff match and the Final.</param>
    /// <param name="existing">The playoff matches and Final that exist.</param>
    public ImmutableArray<NextMatch<TPlayer>> NextMatches<TPlayer>(
        IReadOnlyList<TPlayer> playersInPlaceOrder,
        IReadOnlyDictionary<BracketPosition, IReadOnlyDictionary<TPlayer, Outcome>> decided,
        IReadOnlyCollection<BracketPosition> existing)
        where TPlayer : notnull =>
        [.. Matches.Keys
            .Where(position => !existing.Contains(position))
            .Select(position => (Position: position, Players: PlayersOf(position, playersInPlaceOrder, decided)))
            .Where(match => match.Players is not null)
            .Select(match => new NextMatch<TPlayer>(match.Position, [.. match.Players!.Value.Select(seat => seat.Player)]))];

    /// <summary>
    /// What reopening a confirmed playoff match or Final, or one of its shootouts, deletes: the match's shootouts after
    /// it, and every existing match after the match in the bracket.
    /// </summary>
    /// <param name="shootout">The sequence number of the shootout reopened, or null when the match itself is.</param>
    /// <param name="existingPositions">The playoff matches and Final that exist.</param>
    /// <param name="existingShootouts">The sequence numbers of the match's existing shootouts.</param>
    public ReopenDeletion DeletedByReopening(
        BracketPosition position,
        int? shootout,
        IReadOnlyCollection<BracketPosition> existingPositions,
        IReadOnlyCollection<int> existingShootouts)
    {
        HashSet<BracketPosition> after = [];
        Queue<BracketPosition> toVisit = new([position]);

        while (toVisit.TryDequeue(out BracketPosition current))
        {
            foreach (BracketPosition destination in Taking(current, Outcome.FirstWinner, Outcome.SecondWinner, Outcome.Loser))
            {
                if (after.Add(destination))
                    toVisit.Enqueue(destination);
            }
        }

        return new ReopenDeletion(
            Shootouts: [.. existingShootouts.Where(sequence => sequence > (shootout ?? 0)).Order()],
            Positions: [.. existingPositions.Where(after.Contains).Order()]);
    }

    private IEnumerable<BracketPosition> Taking(BracketPosition source, params Outcome[] outcomes) =>
        Matches
            .Where(match => match.Value.OfType<OutcomeSlot>().Any(slot => slot.Source == source && outcomes.Contains(slot.Outcome)))
            .Select(match => match.Key);

    private static QualificationPlaceSlot Q(int place) => new(place);

    private static OutcomeSlot First(BracketPosition source) => new(source, Outcome.FirstWinner);

    private static OutcomeSlot Second(BracketPosition source) => new(source, Outcome.SecondWinner);

    private static BracketSlot[] Winners(BracketPosition source) => [First(source), Second(source)];

    private static BracketSlot[] Losers(BracketPosition source) =>
        [new OutcomeSlot(source, Outcome.Loser), new OutcomeSlot(source, Outcome.Loser)];
}
