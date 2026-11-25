using Cbo.Core.Validation;
using FluentValidation;

namespace Cbo.Core.TournamentTopics;

/// <summary>
/// Checks the topics a participant brings, as far as the list itself shows: no negative priority position, and no
/// position or topic taken twice. <see cref="SetMyTournamentTopicsCommandHandler"/> checks the rest against the
/// tournament's maximum and the topics themselves.
/// </summary>
public sealed class TournamentTopicListValidator : RequestValidator<List<TournamentTopicRequest>>
{
    public TournamentTopicListValidator()
    {
        // A root RuleForEach would name the paths after the lambda parameter (x[0].priorityIndex), so the body's bare
        // list gets its paths by hand
        RuleFor(topics => topics).Custom((topics, context) =>
        {
            HashSet<int> seenPriorityIndexes = [];
            HashSet<Guid> seenTopicIds = [];

            for (int i = 0; i < topics.Count; i++)
            {
                TournamentTopicRequest? topic = topics[i];
                if (topic is null)
                    continue;

                if (topic.PriorityIndex < 0)
                    context.AddFailure($"[{i}].priorityIndex", "The priority position must be 1 or greater.");
                else if (!seenPriorityIndexes.Add(topic.PriorityIndex))
                    context.AddFailure($"[{i}].priorityIndex", "This priority position appears more than once.");

                if (!seenTopicIds.Add(topic.TopicId))
                    context.AddFailure($"[{i}].topicId", "This topic appears more than once.");
            }
        });
    }

    public override string InvalidMessage => TournamentTopicFailures.InvalidMessage;
}
