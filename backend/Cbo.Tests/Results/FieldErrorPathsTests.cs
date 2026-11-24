using System.Collections.Generic;
using System.Linq;
using Cbo.Core.Domain;
using Cbo.Results;

namespace Cbo.Tests.Results;

public class FieldErrorPathsTests
{
    private static readonly string TooManyQuestions =
        $"A topic cannot have more than {Limits.Topics.QuestionsMax} questions.";

    private static readonly string CostOutOfRange =
        $"Cost must be between {Limits.Topics.QuestionCostMin} and {Limits.Topics.QuestionCostMax}.";

    [Fact]
    public void Prefixes_a_top_level_key()
    {
        IReadOnlyList<FieldError> prefixed = FieldErrorPaths.Prefix(
            [new FieldError("questions", TooManyQuestions)], "topics[2].");

        FieldError error = Assert.Single(prefixed);
        Assert.Equal("topics[2].questions", error.Field);
        Assert.Equal(TooManyQuestions, error.Message);
    }

    [Fact]
    public void Prefixes_indexed_keys_keeping_their_order()
    {
        IReadOnlyList<FieldError> prefixed = FieldErrorPaths.Prefix(
            [
                new FieldError("questions[0].text", "Question text is required."),
                new FieldError("questions[3].cost", CostOutOfRange),
            ],
            "topics[0].");

        Assert.Equal(
            ["topics[0].questions[0].text", "topics[0].questions[3].cost"],
            prefixed.Select(e => e.Field));
        Assert.Equal(
            ["Question text is required.", CostOutOfRange],
            prefixed.Select(e => e.Message));
    }

    [Fact]
    public void Prefixing_no_errors_gives_no_errors()
    {
        Assert.Empty(FieldErrorPaths.Prefix([], "topics[0]."));
    }
}
