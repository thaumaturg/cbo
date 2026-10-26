namespace Cbo.API.Commands.Topics;

/// <summary>
/// A question of a topic being updated. A null <paramref name="Id"/> adds a question. The topic's questions that no
/// <see cref="EditedQuestion"/> refers to are removed.
/// </summary>
public sealed record EditedQuestion(
    Guid? Id,
    int QuestionNumber,
    int CostPositive,
    int CostNegative,
    string Text,
    string Answer,
    string? Comment);
