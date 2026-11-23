using Cbo.Core.Domain;
using Cbo.Results;

namespace Cbo.Core.Topics;

/// <summary>
/// Failures shared by the topic command handlers.
/// </summary>
internal static class TopicFailures
{
    public static Failure Invalid(IEnumerable<FieldError> fieldErrors) =>
        Failure.Invalid("Some of the topic data is invalid.", fieldErrors);

    public static Failure OwnedTopicsCapReached(int total) =>
        Failure.Conflict(
            $"You can own at most {DefaultSettings.OwnedTopicsMax} topics, and this would bring you to {total}.");
}
