using System.Linq;
using Cbo.Core.Participants;

namespace Cbo.Tests.Participants;

public class QualificationPlacesTests
{
    private static int[] SeedsInPlaceOrder(params QualificationResult[] players) =>
        QualificationPlaces.InPlaceOrder(players, player => player).Select(player => player.Seed).ToArray();

    [Fact]
    public void More_points_rank_higher()
    {
        int[] seeds = SeedsInPlaceOrder(
            new QualificationResult(Points: 4m, Score: 90, PositiveScore: 90, Seed: 1),
            new QualificationResult(Points: 7.5m, Score: 10, PositiveScore: 10, Seed: 2),
            new QualificationResult(Points: 5m, Score: 50, PositiveScore: 50, Seed: 3));

        Assert.Equal([2, 3, 1], seeds);
    }

    [Fact]
    public void Equal_points_are_broken_by_score()
    {
        int[] seeds = SeedsInPlaceOrder(
            new QualificationResult(Points: 6m, Score: -10, PositiveScore: 90, Seed: 1),
            new QualificationResult(Points: 6m, Score: 40, PositiveScore: 40, Seed: 2),
            new QualificationResult(Points: 6m, Score: 20, PositiveScore: 60, Seed: 3));

        Assert.Equal([2, 3, 1], seeds);
    }

    [Fact]
    public void Equal_points_and_score_are_broken_by_positive_score()
    {
        int[] seeds = SeedsInPlaceOrder(
            new QualificationResult(Points: 6m, Score: 20, PositiveScore: 20, Seed: 1),
            new QualificationResult(Points: 6m, Score: 20, PositiveScore: 70, Seed: 2),
            new QualificationResult(Points: 6m, Score: 20, PositiveScore: 40, Seed: 3));

        Assert.Equal([2, 3, 1], seeds);
    }

    [Fact]
    public void Equal_points_score_and_positive_score_are_broken_by_the_lower_seed()
    {
        int[] seeds = SeedsInPlaceOrder(
            new QualificationResult(Points: 6m, Score: 20, PositiveScore: 40, Seed: 7),
            new QualificationResult(Points: 6m, Score: 20, PositiveScore: 40, Seed: 2),
            new QualificationResult(Points: 6m, Score: 20, PositiveScore: 40, Seed: 5));

        Assert.Equal([2, 5, 7], seeds);
    }
}
