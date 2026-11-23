namespace Cbo.Core.Topics;

/// <summary>A topic of an import, read from the rows of a spreadsheet.</summary>
public sealed record ImportedTopic(
    string Title,
    string? Description,
    IReadOnlyList<NewQuestion> Questions);
