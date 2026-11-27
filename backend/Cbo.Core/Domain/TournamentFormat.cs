namespace Cbo.Core.Domain;

/// <summary>
/// The shape of a tournament: its size, its qualification layout and, where one exists, its playoff bracket. See
/// docs/adr/0013-format-replaces-tournament-size-as-the-setting.md.
/// </summary>
public enum TournamentFormat
{
    /// <summary>14 players, 3 qualification matches each. Has no playoff bracket yet, so it can't be chosen.</summary>
    Fourteen,

    /// <summary>12 players, 3 qualification matches each.</summary>
    TwelveShort,

    /// <summary>12 players, 4 qualification matches each.</summary>
    TwelveLong,
}

public static class TournamentFormatExtensions
{
    /// <summary>How many players a tournament of this format has.</summary>
    public static int TournamentSize(this TournamentFormat format) => format switch
    {
        TournamentFormat.Fourteen => 14,
        TournamentFormat.TwelveShort or TournamentFormat.TwelveLong => 12,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown tournament format."),
    };
}
