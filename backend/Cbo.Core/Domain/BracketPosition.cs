namespace Cbo.Core.Domain;

/// <summary>
/// A playoff match's place in the bracket, Playoff #1 to #9, or the Final. The value is the match's number in the
/// Playoff.
/// </summary>
public enum BracketPosition
{
    Playoff1 = 1,
    Playoff2 = 2,
    Playoff3 = 3,
    Playoff4 = 4,
    Playoff5 = 5,
    Playoff6 = 6,
    Playoff7 = 7,
    Playoff8 = 8,
    Playoff9 = 9,
    Final = 10,
}
