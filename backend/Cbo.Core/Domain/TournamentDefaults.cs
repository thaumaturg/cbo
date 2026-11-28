namespace Cbo.Core.Domain;

/// <summary>
/// Tournament settings a new tournament gets when the Creator leaves them out.
/// </summary>
public static class TournamentDefaults
{
    /// <summary>Format of a new tournament.</summary>
    public const TournamentFormat Format = TournamentFormat.TwelveShort;

    /// <summary>Fewest topics each participant brings to a new tournament.</summary>
    public const int TopicsPerParticipantMin = 6;

    /// <summary>Most topics each participant brings to a new tournament.</summary>
    public const int TopicsPerParticipantMax = 10;
}
