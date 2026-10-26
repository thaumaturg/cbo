using Cbo.Results;

namespace Cbo.API.CommandHandlers.Rounds;

/// <summary>
/// Failures shared by the round command handlers.
/// </summary>
internal static class RoundFailures
{
    public static Failure InvalidField(string field, string message) =>
        Failure.Invalid("Some of the round data is invalid.", new FieldError(field, message));

    public static Failure MatchNotFound() =>
        Failure.NotFound("Match not found in this tournament.");

    public static Failure RoundNotFound(int roundNumber) =>
        Failure.NotFound($"Round {roundNumber} not found for this match.");

    public static Failure RoundAlreadyExists(int roundNumber) =>
        Failure.Conflict($"Round {roundNumber} already exists for this match. Edit it instead of creating it again.");
}
