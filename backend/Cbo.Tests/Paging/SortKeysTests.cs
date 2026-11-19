using System;
using System.Linq;
using Cbo.Core.Paging;

namespace Cbo.Tests.Paging;

public class SortKeysTests
{
    private sealed record Row(Guid Id, string Title, int QuestionCount);

    private static readonly Guid Id1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid Id2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid Id3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static readonly Guid Id4 = Guid.Parse("00000000-0000-0000-0000-000000000004");

    private static readonly Row[] Rows =
    [
        new(Id3, "Beta", 5),
        new(Id4, "Alpha", 7),
        new(Id1, "Beta", 7),
        new(Id2, "Alpha", 5),
    ];

    private static SortKeys<Row> CreateSortKeys() =>
        new SortKeys<Row>("-questionCount", r => r.Id)
            .Add("title", r => r.Title)
            .Add("questionCount", r => r.QuestionCount);

    private static Guid[] SortedIds(string? sort) =>
        CreateSortKeys().Apply(Rows.AsQueryable(), sort).Select(r => r.Id).ToArray();

    [Theory]
    [InlineData("title")]
    [InlineData("-title")]
    [InlineData("questionCount")]
    [InlineData("-questionCount")]
    public void An_allowed_key_is_valid_with_or_without_a_leading_minus(string sort)
    {
        Assert.True(CreateSortKeys().IsValid(sort));
    }

    [Theory]
    [InlineData("bogus")]
    [InlineData("title,questionCount")]
    [InlineData("title,")]
    [InlineData("-")]
    [InlineData("--title")]
    [InlineData("Title")]
    [InlineData("-QuestionCount")]
    [InlineData(" title")]
    [InlineData("+title")]
    public void Unknown_keys_commas_an_empty_key_after_the_minus_and_wrong_case_are_invalid(string sort)
    {
        Assert.False(CreateSortKeys().IsValid(sort));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void A_missing_or_blank_sort_is_valid_because_it_means_the_default(string? sort)
    {
        Assert.True(CreateSortKeys().IsValid(sort));
    }

    [Fact]
    public void Ascending_sort_orders_by_the_key_then_by_id()
    {
        Assert.Equal([Id2, Id4, Id1, Id3], SortedIds("title"));
    }

    [Fact]
    public void Descending_sort_orders_by_the_key_descending_then_by_id_ascending()
    {
        Assert.Equal([Id1, Id3, Id2, Id4], SortedIds("-title"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void A_missing_or_blank_sort_applies_the_default(string? sort)
    {
        Assert.Equal([Id1, Id4, Id2, Id3], SortedIds(sort));
    }

    [Fact]
    public void Applying_an_invalid_sort_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => SortedIds("bogus"));
    }

    [Fact]
    public void The_unknown_key_message_lists_the_allowed_keys_in_the_order_they_were_added()
    {
        Assert.Equal("Unknown sort key. Allowed: title, questionCount.", CreateSortKeys().UnknownKeyMessage);
    }
}
