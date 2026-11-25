using Cbo.Core.Domain;
using Cbo.Results;

namespace Cbo.Core.Topics;

/// <summary>
/// Failures and messages shared by the topic command handlers and request validators.
/// </summary>
internal static class TopicFailures
{
    public const string InvalidMessage = "Some of the topic data is invalid.";

    public const string InvalidImportMessage = "Some of the topics are invalid.";

    public static Failure Invalid(IEnumerable<FieldError> fieldErrors) =>
        Failure.Invalid(InvalidMessage, fieldErrors);

    public static Failure OwnedTopicsCapReached(int total) =>
        Failure.Conflict(
            $"You can own at most {Limits.Topics.OwnedMax} topics, and this would bring you to {total}.");
}
