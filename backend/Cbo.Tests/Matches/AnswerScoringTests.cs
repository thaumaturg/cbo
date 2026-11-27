using System;
using Cbo.Core.Domain;
using Cbo.Core.Matches;

namespace Cbo.Tests.Matches;

public class AnswerScoringTests
{
    private static RoundAnswer Marked(int cost, bool accepted) => Answer(cost, accepted, overrideCost: null);

    private static RoundAnswer Overridden(int overrideCost) => Answer(cost: 30, accepted: null, overrideCost);

    private static RoundAnswer Answer(int cost, bool? accepted, int? overrideCost) => new()
    {
        RoundId = Guid.NewGuid(),
        QuestionId = Guid.NewGuid(),
        MatchParticipantId = Guid.NewGuid(),
        IsAnswerAccepted = accepted,
        OverrideCost = overrideCost,
        Question = new Question { QuestionNumber = 1, Cost = cost, Text = "Q", Answer = "A", TopicId = Guid.NewGuid() },
    };

    [Fact]
    public void An_accepted_answer_adds_its_cost_and_a_rejected_one_subtracts_it_from_the_score_only()
    {
        AnswerScores scores = AnswerScoring.Sum([Marked(30, accepted: true), Marked(20, accepted: false)]);

        Assert.Equal(new AnswerScores(Score: 10, PositiveScore: 30), scores);
    }

    [Theory]
    [InlineData(25, 25)]
    [InlineData(0, 0)]
    [InlineData(-25, 0)]
    public void An_override_cost_adds_to_the_score_as_is_and_to_the_positive_score_only_above_zero(
        int overrideCost, int expectedPositiveScore)
    {
        AnswerScores scores = AnswerScoring.Sum([Overridden(overrideCost)]);

        Assert.Equal(new AnswerScores(Score: overrideCost, PositiveScore: expectedPositiveScore), scores);
    }
}
