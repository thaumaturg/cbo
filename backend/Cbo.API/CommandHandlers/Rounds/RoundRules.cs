using Cbo.API.Commands.Rounds;
using Cbo.API.Data;
using Cbo.API.Models.Constants;
using Cbo.API.Models.Domain;
using Cbo.Results;
using Microsoft.EntityFrameworkCore;

namespace Cbo.API.CommandHandlers.Rounds;

/// <summary>
/// Round validation and answer building shared by the round command handlers. Reads only.
/// </summary>
internal static class RoundRules
{
    /// <summary>
    /// Checks everything that can be wrong with the round in the request body itself. All of it is
    /// <see cref="FailureKind.Invalid"/>. On success returns the round topic's questions by id, tracked, for
    /// <see cref="ToRoundAnswers"/>.
    /// </summary>
    public static async Task<Result<Dictionary<Guid, Question>>> ValidateAsync(
        CboDbContext dbContext,
        Match match,
        int numberInMatch,
        Guid topicId,
        bool isOverrideMode,
        IReadOnlyList<GivenAnswer> answers,
        CancellationToken cancellationToken)
    {
        if (numberInMatch < 1 || numberInMatch > DefaultSettings.RoundsPerMatch)
            return RoundFailures.InvalidField("numberInMatch", $"Round number must be between 1 and {DefaultSettings.RoundsPerMatch}.");

        Topic? topic = await dbContext.Topics
            .Include(t => t.Questions)
            .FirstOrDefaultAsync(t => t.Id == topicId, cancellationToken);
        if (topic is null)
            return RoundFailures.InvalidField("topicId", "Topic not found.");

        bool isTournamentTopic = await dbContext.TournamentTopics
            .AnyAsync(tt => tt.TournamentId == match.TournamentId && tt.TopicId == topicId, cancellationToken);
        if (!isTournamentTopic)
            return RoundFailures.InvalidField("topicId", "This topic is not part of this tournament.");

        Result answersValidation = ValidateAnswerModes(answers, isOverrideMode);
        if (answersValidation.IsFailure)
            return answersValidation.Failure;

        Dictionary<Guid, Question> questionsById = topic.Questions.ToDictionary(q => q.Id);
        GivenAnswer? foreignQuestionAnswer = answers.FirstOrDefault(a => !questionsById.ContainsKey(a.QuestionId));
        if (foreignQuestionAnswer is not null)
            return RoundFailures.InvalidField("answers", $"Question {foreignQuestionAnswer.QuestionId} is not part of the round's topic.");

        HashSet<Guid> matchParticipantIds = match.MatchParticipants.Select(mp => mp.Id).ToHashSet();
        GivenAnswer? foreignParticipantAnswer = answers.FirstOrDefault(a => !matchParticipantIds.Contains(a.MatchParticipantId));
        if (foreignParticipantAnswer is not null)
            return RoundFailures.InvalidField("answers", $"Participant {foreignParticipantAnswer.MatchParticipantId} is not part of this match.");

        var duplicateAnswer = answers
            .GroupBy(a => new { a.QuestionId, a.MatchParticipantId })
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateAnswer is not null)
            return RoundFailures.InvalidField("answers", $"Participant {duplicateAnswer.Key.MatchParticipantId} has more than one answer to question {duplicateAnswer.Key.QuestionId}.");

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

    private static Result ValidateAnswerModes(IReadOnlyList<GivenAnswer> answers, bool isOverrideMode)
    {
        if (isOverrideMode)
        {
            foreach (GivenAnswer answer in answers)
            {
                if (answer.IsAnswerAccepted.HasValue)
                    return RoundFailures.InvalidField("answers", "In override mode, IsAnswerAccepted must be null for all answers.");

                if (!answer.OverrideCost.HasValue)
                    return RoundFailures.InvalidField("answers", "In override mode, OverrideCost must be provided for all answers.");
            }
        }
        else
        {
            foreach (GivenAnswer answer in answers)
            {
                if (!answer.IsAnswerAccepted.HasValue)
                    return RoundFailures.InvalidField("answers", "In standard mode, IsAnswerAccepted must be provided for all answers.");

                if (answer.OverrideCost.HasValue)
                    return RoundFailures.InvalidField("answers", "In standard mode, OverrideCost must be null for all answers.");
            }

            List<IGrouping<Guid, GivenAnswer>> positiveAnswersByQuestion = answers
                .Where(a => a.IsAnswerAccepted == true)
                .GroupBy(a => a.QuestionId)
                .Where(g => g.Count() > 1)
                .ToList();

            if (positiveAnswersByQuestion.Count > 0)
            {
                Guid questionId = positiveAnswersByQuestion.First().Key;
                return RoundFailures.InvalidField("answers", $"Question {questionId} has multiple correct answers. Only one correct answer is allowed per question.");
            }
        }

        return Result.Success();
    }
}
