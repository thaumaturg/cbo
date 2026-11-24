using Cbo.Core.Domain;
using Cbo.Results;

namespace Cbo.Core.Tournaments;

/// <summary>
/// Tournament settings with defaults filled in. Shared by the create and update handlers.
/// </summary>
internal readonly record struct TournamentSettings(int PlayersPerTournament, int TopicsPerParticipantMax, int TopicsPerParticipantMin)
{
    public static TournamentSettings Resolve(int? playersPerTournament, int? topicsPerParticipantMax, int? topicsPerParticipantMin) =>
        new(
            playersPerTournament ?? TournamentDefaults.PlayersPerTournament,
            topicsPerParticipantMax ?? TournamentDefaults.TopicsPerParticipantMax,
            topicsPerParticipantMin ?? TournamentDefaults.TopicsPerParticipantMin);

    /// <summary>
    /// The tournament form is form-like input, so every problem is collected.
    /// </summary>
    public List<FieldError> Validate()
    {
        List<FieldError> fieldErrors = [];

        if (!Limits.Tournaments.SupportedSizes.Contains(PlayersPerTournament))
            fieldErrors.Add(new FieldError("playersPerTournament", $"Tournament size must be one of: {string.Join(", ", Limits.Tournaments.SupportedSizes)}."));

        if (TopicsPerParticipantMin < Limits.Tournaments.TopicsPerParticipantFloor)
            fieldErrors.Add(new FieldError("topicsPerParticipantMin", "The minimum number of topics can't be negative."));

        if (TopicsPerParticipantMax < TopicsPerParticipantMin)
            fieldErrors.Add(new FieldError("topicsPerParticipantMax", $"The maximum number of topics can't be less than the minimum ({TopicsPerParticipantMin})."));

        return fieldErrors;
    }
}
