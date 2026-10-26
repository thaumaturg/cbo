using Cbo.Results;

namespace Cbo.API.CommandHandlers.Topics;

/// <summary>
/// Failures shared by the topic command handlers.
/// </summary>
internal static class TopicFailures
{
    public static Failure Invalid(IEnumerable<FieldError> fieldErrors) =>
        Failure.Invalid("Some of the topic data is invalid.", fieldErrors);

    public static Failure PlayedAuthorsCannotChange() =>
        Failure.Conflict("This topic has been played in a match, so its authors can't be changed.");
}
