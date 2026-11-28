using Cbo.Core.Matches;
using Cbo.Results;

namespace Cbo.Core.Rounds;

/// <summary>
/// Failures shared by the round command handlers and request validators.
/// </summary>
internal static class RoundFailures
{
    public const string InvalidMessage = "Some of the round data is invalid.";

    public static Failure InvalidField(string field, string message) =>
        Failure.Invalid(InvalidMessage, new FieldError(field, message));

    public static Failure RoundNotFound(int roundNumber) =>
        Failure.NotFound($"Round {roundNumber} not found for this match.");

    public static Failure RoundAlreadyExists(int roundNumber) =>
        Failure.Conflict($"Round {roundNumber} already exists for this match. Edit it instead of creating it again.");

    public static Failure MatchLocked(MatchLockReason reason) => Failure.Conflict(reason switch
    {
        MatchLockReason.Confirmed => "This match is confirmed, so its rounds can't be added, changed or deleted.",
        MatchLockReason.PlayoffStarted => "The Playoff has started, so the rounds of qualification matches can't be added, changed or deleted.",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown match lock reason."),
    });
}
