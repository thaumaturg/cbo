using Cbo.Results;

namespace Cbo.Core.Matches;

/// <summary>
/// Failures shared by the handlers that work on one match of a tournament.
/// </summary>
internal static class MatchFailures
{
    public static Failure MatchNotFound() =>
        Failure.NotFound("Match not found in this tournament.");
}
