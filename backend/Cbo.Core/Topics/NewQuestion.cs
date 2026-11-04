namespace Cbo.Core.Topics;

/// <summary>A question of a topic being created.</summary>
public sealed record NewQuestion(
    int QuestionNumber,
    int Cost,
    string Text,
    string Answer,
    string? Comment);
