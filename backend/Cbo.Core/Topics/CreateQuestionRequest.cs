using Cbo.Core.Validation;

namespace Cbo.Core.Topics;

public record CreateQuestionRequest : IQuestionRequest
{
    public required int QuestionNumber { get; set; }
    public required int Cost { get; set; }
    public required string Text { get; set => field = RequestText.Trim(value); }
    public required string Answer { get; set => field = RequestText.Trim(value); }
    public string? Comment { get; set => field = RequestText.TrimToNull(value); }
}
