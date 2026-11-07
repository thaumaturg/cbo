using Cbo.Core.Cqrs;

namespace Cbo.Core.Topics;

/// <summary>
/// Replaces the topic's details, questions and co-authors. <paramref name="IsAuthor"/> applies to the current user.
/// <paramref name="CoAuthorUsernames"/> are the co-authors; the owner is never one of them.
/// </summary>
public sealed record UpdateTopicCommand(
    Guid TopicId,
    string Title,
    string? Description,
    bool IsAuthor,
    IReadOnlyList<EditedQuestion> Questions,
    IReadOnlyList<string> CoAuthorUsernames) : ICommand;
