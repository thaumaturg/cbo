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

    public static Failure InvalidImport(IEnumerable<FieldError> fieldErrors) =>
        Failure.Invalid("Some of the topics are invalid.", fieldErrors);

    /// <summary>
    /// Repeats the size error as the message, since it is about the whole import rather than one of its topics.
    /// </summary>
    public static Failure InvalidImportSize(FieldError sizeError) =>
        Failure.Invalid(sizeError.Message, sizeError);

    public static Failure OwnedTopicsCapReached(int total) =>
        Failure.Conflict(
            $"You can own at most {Limits.Topics.OwnedMax} topics, and this would bring you to {total}.");
}
