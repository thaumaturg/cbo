using Cbo.Core.Validation;

namespace Cbo.Core.Topics;

public record ImportTopicRequest
{
    public required string Title { get; set => field = RequestText.Trim(value); }
    public string? Description { get; set => field = RequestText.TrimToNull(value); }
    public required ICollection<CreateQuestionRequest> Questions { get; set; }
}
