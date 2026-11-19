using System;
using System.Linq;
using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Core.Paging;
using Cbo.Core.Topics;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Tests.Topics;

public class TopicListFilterTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static GetTopicsQuery Query(
        string? sort = null,
        string? search = null,
        bool? played = null,
        bool? isAuthor = null) =>
        new(PagingDefaults.FirstPage, PagingDefaults.DefaultPageSize, sort, search, played, isAuthor);

    private static GetTopicsQuery EveryFilterSet(string? sort = null) =>
        Query(sort, search: "50%_off\\", played: false, isAuthor: true);

    // Builds the SQL of the items query the way ToPageAsync runs it, without opening a connection
    private static string ItemsSql(GetTopicsQuery query)
    {
        using CboDbContext dbContext = CreateDbContext();

        IQueryable<Topic> filtered = TopicListFilter.Apply(dbContext.Topics, query, CurrentUserId);

        return TopicListFilter.SortKeys.Apply(filtered, query.Sort)
            .Skip(25)
            .Take(query.PageSize)
            .Select(TopicProjections.ToListItemResponse(CurrentUserId))
            .ToQueryString();
    }

    // The SQL of the filters alone, so the projection's own subqueries don't show up in the assertions
    private static string FilterSql(GetTopicsQuery query)
    {
        using CboDbContext dbContext = CreateDbContext();

        return TopicListFilter.Apply(dbContext.Topics, query, CurrentUserId).ToQueryString();
    }

    private static CboDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<CboDbContext>()
            .UseNpgsql("Host=localhost")
            .UseSnakeCaseNamingConvention()
            .Options);

    [Fact]
    public void Every_filter_set_translates_to_sql_with_an_escaped_ilike_on_the_title()
    {
        string sql = ItemsSql(EveryFilterSet());

        Assert.Contains("t.title ILIKE", sql);
        Assert.Contains(@"ESCAPE '\'", sql);
        Assert.Contains(@"'%50\%\_off\\%'", sql);
    }

    [Fact]
    public void Only_topics_the_current_user_owns_are_listed()
    {
        string sql = FilterSql(Query());

        Assert.Contains(".is_owner", sql);
        Assert.DoesNotContain("rounds", sql);
        Assert.DoesNotContain(".is_author", sql);
    }

    [Fact]
    public void The_played_filter_keeps_topics_with_rounds()
    {
        string sql = FilterSql(Query(played: true));

        Assert.Matches(@"EXISTS \(\s+SELECT 1\s+FROM rounds", sql);
        Assert.DoesNotMatch(@"NOT EXISTS \(\s+SELECT 1\s+FROM rounds", sql);
    }

    [Fact]
    public void The_not_played_filter_keeps_topics_without_rounds()
    {
        string sql = FilterSql(Query(played: false));

        Assert.Matches(@"NOT EXISTS \(\s+SELECT 1\s+FROM rounds", sql);
    }

    [Fact]
    public void The_is_author_filter_keeps_topics_the_current_user_is_an_author_of()
    {
        string sql = FilterSql(Query(isAuthor: true));

        Assert.Matches(@"EXISTS \(\s+SELECT 1\s+FROM topic_authors AS \w+\s+WHERE [^)]*\.is_author\)", sql);
        Assert.DoesNotMatch(@"NOT EXISTS \(\s+SELECT 1\s+FROM topic_authors", sql);
    }

    [Fact]
    public void The_not_an_author_filter_keeps_topics_the_current_user_is_not_an_author_of()
    {
        string sql = FilterSql(Query(isAuthor: false));

        Assert.Matches(@"NOT EXISTS \(\s+SELECT 1\s+FROM topic_authors AS \w+\s+WHERE [^)]*\.is_author\)", sql);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_or_blank_search_adds_no_title_filter(string? search)
    {
        string sql = FilterSql(Query(search: search));

        Assert.DoesNotContain("ILIKE", sql);
    }

    [Theory]
    [InlineData("title", @"ORDER BY t\.title, t\.id")]
    [InlineData("-title", @"ORDER BY t\.title DESC, t\.id")]
    [InlineData("questionCount", @"ORDER BY \(\s+SELECT count\(\*\)::int\s+FROM questions AS \w+\s+WHERE t\.id = \w+\.topic_id\), t\.id")]
    [InlineData("-questionCount", @"ORDER BY \(\s+SELECT count\(\*\)::int\s+FROM questions AS \w+\s+WHERE t\.id = \w+\.topic_id\) DESC, t\.id")]
    [InlineData("createdAt", @"ORDER BY t\.created_at, t\.id")]
    [InlineData("-createdAt", @"ORDER BY t\.created_at DESC, t\.id")]
    [InlineData(null, @"ORDER BY t\.created_at DESC, t\.id")]
    public void Each_sort_key_orders_by_its_column_then_by_id(string? sort, string expectedOrderBy)
    {
        string sql = ItemsSql(EveryFilterSet(sort));

        Assert.Matches(expectedOrderBy, sql);
    }

    [Fact]
    public void The_sort_keys_are_title_question_count_and_created_at()
    {
        Assert.Equal("Unknown sort key. Allowed: title, questionCount, createdAt.", TopicListFilter.SortKeys.UnknownKeyMessage);
    }
}
