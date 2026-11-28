using Cbo.Results;

namespace Cbo.Core.Playoffs;

/// <summary>
/// Failures shared by the playoff handlers: Confirm, Reopen and the query listing what Reopen deletes.
/// </summary>
internal static class PlayoffFailures
{
    public const string ReopenForbiddenMessage = "You don't have permission to reopen matches in this tournament.";

    /// <param name="action">What is done to the match, such as "confirmed".</param>
    public static Failure NotInPlayoff(string action) =>
        Failure.Conflict($"Matches can only be {action} while the tournament is in the Playoff stage.");

    /// <param name="action">What is done to the match, such as "confirmed".</param>
    public static Failure QualificationMatch(string action) =>
        Failure.Conflict($"Only playoff matches, shootouts and the Final are {action}.");
}
