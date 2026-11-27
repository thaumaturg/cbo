using Cbo.Core.Domain;

namespace Cbo.Core.Playoffs;

/// <summary>One player's seat in a playoff match or the Final: where the player who takes it comes from.</summary>
public abstract record BracketSlot
{
    /// <summary>Where the player comes from, as shown next to them, such as "Q4", "1st of #2" or "dropped from #8".</summary>
    public abstract string Label { get; }
}

/// <summary>The player at this qualification place, 1 being the best.</summary>
public sealed record QualificationPlaceSlot(int Place) : BracketSlot
{
    public override string Label => $"Q{Place}";
}

/// <summary>
/// A player with this <see cref="Domain.Outcome"/> in the source match. Losers fill one slot each, so a match taking
/// both losers of a match has two equal slots.
/// </summary>
public sealed record OutcomeSlot(BracketPosition Source, Outcome Outcome) : BracketSlot
{
    public override string Label => Outcome switch
    {
        Outcome.FirstWinner => $"1st of #{(int)Source}",
        Outcome.SecondWinner => $"2nd of #{(int)Source}",
        Outcome.Loser => $"dropped from #{(int)Source}",
        _ => throw new InvalidOperationException($"No playoff match or Final takes a player with outcome {Outcome}."),
    };
}
