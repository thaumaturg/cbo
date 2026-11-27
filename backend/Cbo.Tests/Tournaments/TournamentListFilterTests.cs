using System;
using System.Linq;
using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Core.Paging;
using Cbo.Core.Tournaments;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Tests.Tournaments;

public class TournamentListFilterTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static GetTournamentsQuery Query(
        string? sort = null,
        string? search = null,
        TournamentStage? stage = null,
        TournamentParticipantRole[]? roles = null) =>
        new(PagingDefaults.FirstPage, PagingDefaults.DefaultPageSize, sort, search, stage, roles ?? []);

    private static GetTournamentsQuery EveryFilterSet(string? sort = null) =>
        Query(sort, search: "50%_off\\", stage: TournamentStage.Playoff,
            roles: [TournamentParticipantRole.Creator, TournamentParticipantRole.Organizer]);

    // Builds the SQL of the items query the way ToPageAsync runs it, without opening a connection
    private static string ItemsSql(GetTournamentsQuery query)
    {
        using CboDbContext dbContext = CreateDbContext();

        IQueryable<Tournament> filtered = TournamentListFilter.Apply(dbContext.Tournaments, query, CurrentUserId);

        return TournamentListFilter.SortKeys.Apply(filtered, query.Sort)
            .Skip(25)
            .Take(query.PageSize)
            .Select(TournamentProjections.ToResponse(CurrentUserId))
            .ToQueryString();
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
    public void The_role_filter_limits_the_current_users_participation_to_the_chosen_roles()
    {
        string sql = ItemsSql(EveryFilterSet());

        Assert.Contains("role = ANY (@roles)", sql);
        Assert.Contains("'Creator'", sql);
        Assert.Contains("'Organizer'", sql);
        Assert.DoesNotContain("'Player'", sql);
    }

    [Theory]
    [InlineData(TournamentStage.Preparations, "'Preparations'")]
    [InlineData(TournamentStage.Qualifications, "'Qualifications'")]
    [InlineData(TournamentStage.Playoff, "'Playoff'")]
    [InlineData(TournamentStage.Ended, "'Ended'")]
    public void A_stage_filter_keeps_only_that_stage(TournamentStage stage, string storedStage)
    {
        string sql = ItemsSql(Query(stage: stage));

        Assert.Contains("t.current_stage = @stage", sql);
        Assert.Contains($"@stage={storedStage}", sql);
    }

    [Fact]
    public void No_stage_filter_keeps_every_stage()
    {
        string sql = ItemsSql(Query());

        Assert.DoesNotContain("current_stage =", sql);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_or_blank_search_adds_no_title_filter(string? search)
    {
        string sql = ItemsSql(Query(search: search));

        Assert.DoesNotContain("ILIKE", sql);
    }

    [Theory]
    [InlineData("title", "ORDER BY t.title, t.id")]
    [InlineData("-title", "ORDER BY t.title DESC, t.id")]
    [InlineData("createdAt", "ORDER BY t.created_at, t.id")]
    [InlineData("-createdAt", "ORDER BY t.created_at DESC, t.id")]
    [InlineData(null, "ORDER BY t.created_at DESC, t.id")]
    public void Each_sort_key_orders_by_its_column_then_by_id(string? sort, string expectedOrderBy)
    {
        string sql = ItemsSql(EveryFilterSet(sort));

        Assert.Contains(expectedOrderBy, sql);
    }

    // current_stage is stored as a string, so sorting the column itself would put Ended first
    [Theory]
    [InlineData("stage", "END, t.id")]
    [InlineData("-stage", "END DESC, t.id")]
    public void The_stage_sort_follows_the_stage_order_then_id(string sort, string expectedOrderByEnd)
    {
        string sql = ItemsSql(EveryFilterSet(sort));
        string orderBy = sql[sql.IndexOf("ORDER BY", StringComparison.Ordinal)..];

        Assert.StartsWith("ORDER BY CASE", orderBy);
        Assert.Contains(expectedOrderByEnd, orderBy);
        int[] positions =
        [
            orderBy.IndexOf("'Preparations' THEN 0", StringComparison.Ordinal),
            orderBy.IndexOf("'Qualifications' THEN 1", StringComparison.Ordinal),
            orderBy.IndexOf("'Playoff' THEN 2", StringComparison.Ordinal),
            orderBy.IndexOf("'Ended' THEN 3", StringComparison.Ordinal),
        ];
        Assert.DoesNotContain(-1, positions);
        Assert.Equal(positions.Order(), positions);
    }

    [Fact]
    public void The_sort_keys_are_title_stage_and_created_at()
    {
        Assert.Equal("Unknown sort key. Allowed: title, stage, createdAt.", TournamentListFilter.SortKeys.UnknownKeyMessage);
    }
}
