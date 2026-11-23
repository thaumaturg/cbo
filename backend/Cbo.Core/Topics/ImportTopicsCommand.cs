using Cbo.Core.Cqrs;

namespace Cbo.Core.Topics;

/// <summary>
/// Creates <paramref name="Topics"/> at once, all owned by the current user, or none of them.
/// <paramref name="IsAuthor"/> says whether the owner also wrote them. Imported topics have no co-authors.
/// Returns how many topics were created.
/// </summary>
public sealed record ImportTopicsCommand(bool IsAuthor, IReadOnlyList<ImportedTopic> Topics) : ICommand<int>;
