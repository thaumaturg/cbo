namespace Cbo.API.Repositories;

/// <summary>
/// Parameters for updating a topic. This is a repository-layer object, not a DTO.
/// </summary>
public class UpdateTopicParameters
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required bool IsAuthor { get; init; }
    public required IReadOnlyList<UpdateQuestionParameters> Questions { get; init; }
}

/// <summary>
/// Parameters for updating a question within a topic.
/// </summary>
public class UpdateQuestionParameters
{
    public required Guid Id { get; init; }
    public required int QuestionNumber { get; init; }
    public required int CostPositive { get; init; }
    public required int CostNegative { get; init; }
    public required string Text { get; init; }
    public required string Answer { get; init; }
    public string? Comment { get; init; }
}
