using Cbo.Core.Cqrs;

namespace Cbo.Core.Commands.Topics;

/// <summary>Creates a topic owned by the current user. <paramref name="IsAuthor"/> says whether the owner also wrote it.</summary>
public sealed record CreateTopicCommand(
    string Title,
    string? Description,
    bool IsAuthor,
    IReadOnlyList<NewQuestion> Questions) : ICommand<Guid>;
