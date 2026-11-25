using Cbo.Core.Validation;

namespace Cbo.Core.Topics;

public record UpdateTopicRequest
{
    public required string Title { get; set => field = RequestText.Trim(value); }
    public string? Description { get; set => field = RequestText.TrimToNull(value); }
    public required bool IsAuthor { get; set; }
    public required ICollection<UpdateQuestionRequest> Questions { get; set; }
    public required ICollection<TopicAuthorRequest> Authors { get; set; }
}
