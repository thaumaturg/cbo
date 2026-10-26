namespace Cbo.API.Commands.Topics;

/// <summary>A question of a topic being created.</summary>
public sealed record NewQuestion(
    int QuestionNumber,
    int CostPositive,
    int CostNegative,
    string Text,
    string Answer,
    string? Comment);
