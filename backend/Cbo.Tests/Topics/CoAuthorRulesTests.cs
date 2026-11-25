using System;
using System.Linq;
using Cbo.Core.Topics;
using Cbo.Results;

namespace Cbo.Tests.Topics;

public class CoAuthorRulesTests
{
    private static readonly Guid s_ownerId = Guid.NewGuid();

    [Fact]
    public void Co_authors_with_accounts_other_than_the_owner_are_fine()
    {
        Assert.Empty(CoAuthorRules.CheckAccounts(
            [new CoAuthorAccount("anna", Guid.NewGuid()), new CoAuthorAccount("bob", Guid.NewGuid())], s_ownerId));
    }

    [Fact]
    public void A_username_without_an_account_is_reported_at_its_index_by_name()
    {
        FieldError error = Assert.Single(CoAuthorRules.CheckAccounts(
            [new CoAuthorAccount("anna", Guid.NewGuid()), new CoAuthorAccount("nobody", null)], s_ownerId));

        Assert.Equal("authors[1].username", error.Field);
        Assert.Equal("There's no account with the username 'nobody'.", error.Message);
    }

    [Fact]
    public void The_owner_cannot_be_a_co_author()
    {
        FieldError error = Assert.Single(CoAuthorRules.CheckAccounts(
            [new CoAuthorAccount("me", s_ownerId)], s_ownerId));

        Assert.Equal("authors[0].username", error.Field);
        Assert.Equal("The owner can't be added as a co-author.", error.Message);
    }

    [Fact]
    public void Every_problem_is_reported()
    {
        Assert.Equal(
            ["authors[0].username", "authors[2].username"],
            CoAuthorRules.CheckAccounts(
                [
                    new CoAuthorAccount("me", s_ownerId),
                    new CoAuthorAccount("anna", Guid.NewGuid()),
                    new CoAuthorAccount("nobody", null),
                ],
                s_ownerId).Select(e => e.Field));
    }
}
