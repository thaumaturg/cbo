using Cbo.API.Models.Constants;
using Cbo.Results;

namespace Cbo.API.CommandHandlers.Participants;

/// <summary>
/// Failures shared by the participant command handlers.
/// </summary>
internal static class ParticipantFailures
{
    public static Failure InvalidField(string field, string message) =>
        Failure.Invalid("Some of the participant data is invalid.", new FieldError(field, message));

    public static Failure NotFound() =>
        Failure.NotFound("Participant not found in this tournament.");

    public static Failure Locked() =>
        Failure.Conflict("Participants can only be changed during the Preparations stage.");

    public static Failure TooManyOrganizers() =>
        Failure.Conflict($"A tournament can have at most {DefaultSettings.OrganizersPerTournamentMax} organizers.");

    public static Failure AlreadyAParticipant(string username) =>
        Failure.Conflict($"User '{username}' is already a participant in this tournament.");
}
