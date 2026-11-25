using Cbo.Results;

namespace Cbo.Core.Participants;

/// <summary>
/// Failures shared by the participant command handlers.
/// </summary>
internal static class ParticipantFailures
{
    public const string InvalidMessage = "Some of the participant data is invalid.";
    public const string InvalidSeedingMessage = "Some of the seeding data is invalid.";
    public const string SeedingListsEachPlayerOnceMessage = "The seeding must list each player of the tournament exactly once.";

    public static Failure InvalidField(string field, string message) =>
        Failure.Invalid(InvalidMessage, new FieldError(field, message));

    public static Failure SeedingDoesNotListEachPlayerOnce() =>
        Failure.Invalid(InvalidSeedingMessage, new FieldError("orderedParticipantIds", SeedingListsEachPlayerOnceMessage));

    public static Failure NotFound() =>
        Failure.NotFound("Participant not found in this tournament.");

    public static Failure Locked() =>
        Failure.Conflict("Participants can only be changed during the Preparations stage.");

    public static Failure AlreadyAParticipant(string username) =>
        Failure.Conflict($"'{username}' is already a participant in this tournament.");
}
