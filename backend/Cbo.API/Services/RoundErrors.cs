using Cbo.API.Models.Constants;
using Cbo.Results;

namespace Cbo.API.Services;

/// <summary>
/// The client-actionable failures of the rounds flow. Each <see cref="Error.Code"/> is part of the API contract
/// (surfaced as <c>errorCodes</c> in problem details) and must not change once published.
/// </summary>
internal static class RoundErrors
{
    public static Error NumberOutOfRange() => new(
        "numberInMatch",
        $"Round number must be between 1 and {DefaultSettings.RoundsPerMatch}.",
        "round.numberOutOfRange");

    public static Error NumberMismatch() => new(
        "numberInMatch",
        "Round number in URL must match round number in body.",
        "round.numberMismatch");

    public static Error AlreadyExists(int numberInMatch) => new(
        "numberInMatch",
        $"Round {numberInMatch} already exists for this match. Use PUT to update.",
        "round.alreadyExists");

    public static Error NotFound(int numberInMatch) => new(
        string.Empty,
        $"Round {numberInMatch} not found for this match.",
        "round.notFound");

    public static Error Immutable() => new(
        string.Empty,
        "Cannot change the topic or mode of an existing round. Delete it and create a new one.",
        "round.immutable");

    public static Error TopicNotFound() => new(
        "topicId",
        "Topic not found.",
        "topic.notFound");

    public static Error AnswerAcceptedNotAllowed() => Answers(
        "In override mode, IsAnswerAccepted must be null for all answers.",
        "round.answerAcceptedNotAllowed");

    public static Error OverrideCostRequired() => Answers(
        "In override mode, OverrideCost must be provided for all answers.",
        "round.overrideCostRequired");

    public static Error AnswerAcceptedRequired() => Answers(
        "In standard mode, IsAnswerAccepted must be provided for all answers.",
        "round.answerAcceptedRequired");

    public static Error OverrideCostNotAllowed() => Answers(
        "In standard mode, OverrideCost must be null for all answers.",
        "round.overrideCostNotAllowed");

    public static Error MultipleCorrectAnswers(Guid questionId) => Answers(
        $"Question {questionId} has multiple correct answers. Only one correct answer is allowed per question.",
        "round.multipleCorrectAnswers");

    private static Error Answers(string message, string code) => new("answers", message, code);
}
