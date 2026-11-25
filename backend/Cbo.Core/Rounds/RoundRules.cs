using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Rounds;

/// <summary>
/// Round validation and answer building shared by the round command handlers. Reads only.
/// </summary>
internal static class RoundRules
{
    /// <summary>
    /// Checks the parts of the round that need the database: the topic, and whose questions and participants the
    /// answers are about. All of it is <see cref="FailureKind.Invalid"/>. The request validators have already checked
    /// what the body shows on its own. On success returns the round topic's questions by id, tracked, for
    /// <see cref="ToRoundAnswers"/>.
    /// </summary>
    public static async Task<Result<Dictionary<Guid, Question>>> ValidateAsync(
        CboDbContext dbContext,
        Match match,
        Guid topicId,
        IReadOnlyList<GivenAnswer> answers,
        CancellationToken cancellationToken)
    {
        Topic? topic = await dbContext.Topics
            .Include(t => t.Questions)
            .FirstOrDefaultAsync(t => t.Id == topicId, cancellationToken);
        if (topic is null)
            return RoundFailures.InvalidField("topicId", "Topic not found.");

        bool isTournamentTopic = await dbContext.TournamentTopics
            .AnyAsync(tt => tt.TournamentId == match.TournamentId && tt.TopicId == topicId, cancellationToken);
        if (!isTournamentTopic)
            return RoundFailures.InvalidField("topicId", "This topic is not part of this tournament.");

        Dictionary<Guid, Question> questionsById = topic.Questions.ToDictionary(q => q.Id);
        GivenAnswer? foreignQuestionAnswer = answers.FirstOrDefault(a => !questionsById.ContainsKey(a.QuestionId));
        if (foreignQuestionAnswer is not null)
            return RoundFailures.InvalidField("answers", $"Question {foreignQuestionAnswer.QuestionId} is not part of the round's topic.");

        HashSet<Guid> matchParticipantIds = match.MatchParticipants.Select(mp => mp.Id).ToHashSet();
        GivenAnswer? foreignParticipantAnswer = answers.FirstOrDefault(a => !matchParticipantIds.Contains(a.MatchParticipantId));
        if (foreignParticipantAnswer is not null)
            return RoundFailures.InvalidField("answers", $"Participant {foreignParticipantAnswer.MatchParticipantId} is not part of this match.");

        return questionsById;
    }

    /// <summary>
    /// Builds the round's answers. Each gets its tracked question, which the score recalculation reads.
    /// Requires every question id to be a key of <paramref name="questionsById"/>, as <see cref="ValidateAsync"/> ensures.
    /// </summary>
    public static List<RoundAnswer> ToRoundAnswers(
        IReadOnlyList<GivenAnswer> answers,
        Guid roundId,
        IReadOnlyDictionary<Guid, Question> questionsById) =>
        answers
            .Select(a => new RoundAnswer
            {
                IsAnswerAccepted = a.IsAnswerAccepted,
                OverrideCost = a.OverrideCost,
                RoundId = roundId,
                QuestionId = a.QuestionId,
                Question = questionsById[a.QuestionId],
                MatchParticipantId = a.MatchParticipantId,
            })
            .ToList();
}
