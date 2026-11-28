namespace Cbo.Core.Domain;

/// <summary>What a player got from a Decided playoff match or Final, see the glossary.</summary>
public enum Outcome
{
    /// <summary>The higher ranked of a playoff match's two winners.</summary>
    FirstWinner,

    /// <summary>The lower ranked of a playoff match's two winners.</summary>
    SecondWinner,

    /// <summary>One of a playoff match's two losers. Losers are not ordered.</summary>
    Loser,

    /// <summary>1st place in the Final: the Champion.</summary>
    FirstPlace,

    /// <summary>2nd place in the Final.</summary>
    SecondPlace,

    /// <summary>3rd place in the Final.</summary>
    ThirdPlace,

    /// <summary>4th place in the Final.</summary>
    FourthPlace,
}
