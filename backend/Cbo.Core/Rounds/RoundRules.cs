using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Core.Matches;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Rounds;

/// <summary>
/// Round validation and answer building shared by the round command handlers. Reads only.
/// </summary>
internal static class RoundRules
{
    /// <summary>
    /// The round number is within the rounds of the match's kind. The request validators have already checked it is
    /// at least 1. Null when it is.
    /// </summary>
    public static Failure? CheckRoundNumber(MatchType type, int numberInMatch)
    {
        int rounds = type.RoundsToPlay();
        if (numberInMatch >= 1 && numberInMatch <= rounds)
            return null;

        return RoundFailures.InvalidField("numberInMatch", $"Round number must be between 1 and {rounds} for this match.");
    }

    /// <summary>The match's rounds can still be added, changed or deleted. Null when they can.</summary>
    public static Failure? CheckNotLocked(Match match, Tournament tournament) =>
        MatchLock.ReasonFor(match.Type, match.ConfirmedAt, tournament.CurrentStage) is MatchLockReason reason
            ? RoundFailures.MatchLocked(reason)
            : null;

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
