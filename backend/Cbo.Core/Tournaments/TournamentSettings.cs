using Cbo.Core.Domain;

namespace Cbo.Core.Tournaments;

/// <summary>
/// Tournament settings with defaults filled in. Shared by the create and update handlers. The request validators
/// check the settings, with the same defaults filled in, before the handlers run.
/// </summary>
internal readonly record struct TournamentSettings(int TournamentSize, int TopicsPerParticipantMax, int TopicsPerParticipantMin)
{
    public static TournamentSettings Resolve(int? tournamentSize, int? topicsPerParticipantMax, int? topicsPerParticipantMin) =>
        new(
            tournamentSize ?? TournamentDefaults.TournamentSize,
            topicsPerParticipantMax ?? TournamentDefaults.TopicsPerParticipantMax,
            topicsPerParticipantMin ?? TournamentDefaults.TopicsPerParticipantMin);
}
