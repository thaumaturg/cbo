using Cbo.Core.Domain;

namespace Cbo.Core.Topics;

/// <summary>
/// Builds a new topic the way every create path needs it: its questions and the owner's author entry.
/// </summary>
internal static class TopicFactory
{
    /// <summary>
    /// Builds a topic owned by <paramref name="ownerId"/>, who also wrote it when <paramref name="isAuthor"/> is set.
    /// Co-authors aren't added; the caller adds them, if any, to <see cref="Topic.TopicAuthors"/>.
    /// </summary>
    public static Topic Create(
        string title,
        string? description,
        IEnumerable<NewQuestion> questions,
        Guid ownerId,
        bool isAuthor)
    {
        // Set here rather than by the database default, so the id is known before the save
        var topic = new Topic
        {
            Id = Guid.CreateVersion7(),
            Title = title,
            Description = description,
        };

        foreach (NewQuestion question in questions)
        {
            topic.Questions.Add(new Question
            {
                QuestionNumber = question.QuestionNumber,
                Cost = question.Cost,
                Text = question.Text,
                Answer = question.Answer,
                Comment = question.Comment,
                TopicId = topic.Id,
            });
        }

        topic.TopicAuthors.Add(new TopicAuthor
        {
            IsOwner = true,
            IsAuthor = isAuthor,
            ApplicationUserId = ownerId,
            TopicId = topic.Id,
        });

        return topic;
    }
}
