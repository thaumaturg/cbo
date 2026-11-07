using Cbo.Core.Cqrs;

namespace Cbo.Core.Topics;

/// <summary>
/// Creates a topic owned by the current user. <paramref name="IsAuthor"/> says whether the owner also wrote it.
/// <paramref name="CoAuthorUsernames"/> are the co-authors; the owner is never one of them.
/// </summary>
public sealed record CreateTopicCommand(
    string Title,
    string? Description,
    bool IsAuthor,
    IReadOnlyList<NewQuestion> Questions,
    IReadOnlyList<string> CoAuthorUsernames) : ICommand<Guid>;
