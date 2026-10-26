using Cbo.API.Cqrs;

namespace Cbo.API.Commands.Topics;

/// <summary>Replaces the topic's details and questions. <paramref name="IsAuthor"/> applies to the current user.</summary>
public sealed record UpdateTopicCommand(
    Guid TopicId,
    string Title,
    string? Description,
    bool IsAuthor,
    IReadOnlyList<EditedQuestion> Questions) : ICommand;
