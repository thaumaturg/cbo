using Cbo.API.Models.Constants;
using Cbo.Results;

namespace Cbo.API.CommandHandlers.Tournaments;

/// <summary>
/// Tournament settings with defaults filled in. Shared by the create and update handlers.
/// </summary>
internal readonly record struct TournamentSettings(int PlayersPerTournament, int TopicsPerParticipantMax, int TopicsPerParticipantMin)
{
    public static TournamentSettings Resolve(int? playersPerTournament, int? topicsPerParticipantMax, int? topicsPerParticipantMin) =>
        new(
            playersPerTournament ?? DefaultSettings.PlayersPerTournament,
            topicsPerParticipantMax ?? DefaultSettings.TopicsPerParticipantMax,
            topicsPerParticipantMin ?? DefaultSettings.TopicsPerParticipantMin);

    /// <summary>
    /// The tournament form is form-like input, so every problem is collected.
    /// </summary>
    public List<FieldError> Validate()
    {
        List<FieldError> fieldErrors = [];

        if (PlayersPerTournament != DefaultSettings.PlayersPerTournament)
            fieldErrors.Add(new FieldError("playersPerTournament", $"Only {DefaultSettings.PlayersPerTournament} players per tournament are currently supported."));

        if (TopicsPerParticipantMin < 0)
            fieldErrors.Add(new FieldError("topicsPerParticipantMin", "The minimum number of topics can't be negative."));

        if (TopicsPerParticipantMax < TopicsPerParticipantMin)
            fieldErrors.Add(new FieldError("topicsPerParticipantMax", $"The maximum number of topics can't be less than the minimum ({TopicsPerParticipantMin})."));

        return fieldErrors;
    }
}
