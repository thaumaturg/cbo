namespace Cbo.Core.Topics;

/// <summary>
/// The question fields of a create, update or import request, so <see cref="QuestionRequestValidator"/> checks them
/// all the same way.
/// </summary>
public interface IQuestionRequest
{
    int QuestionNumber { get; }
    int Cost { get; }
    string Text { get; }
    string Answer { get; }
    string? Comment { get; }
}
