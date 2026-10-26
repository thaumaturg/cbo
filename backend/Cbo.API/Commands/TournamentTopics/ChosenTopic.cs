namespace Cbo.API.Commands.TournamentTopics;

/// <summary>A topic the current participant brings to the tournament. A lower <paramref name="PriorityIndex"/> is played earlier.</summary>
public sealed record ChosenTopic(Guid TopicId, int PriorityIndex);
