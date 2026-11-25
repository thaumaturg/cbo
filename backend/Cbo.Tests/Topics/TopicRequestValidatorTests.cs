using System.Linq;
using Cbo.Core.Domain;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Topics;

/// <summary>
/// The rules of a create or update topic request: those every topic shares, plus the co-authors.
/// </summary>
public abstract class TopicRequestValidatorTests<TRequest> : TopicContentValidatorTests<TRequest>
{
    protected override string Prefix => "";

    [Fact]
    public void A_topic_can_have_as_many_co_authors_as_the_maximum()
    {
        Validate(ValidTopic with { Authors = Usernames(Limits.Topics.CoAuthorsMax) })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_topic_with_one_co_author_over_the_maximum_has_too_many()
    {
        Validate(ValidTopic with { Authors = Usernames(Limits.Topics.CoAuthorsMax + 1) })
            .ShouldHaveValidationErrorFor("authors")
            .WithErrorMessage($"A topic can have at most {Limits.Topics.CoAuthorsMax} co-authors.")
            .Only();
    }

    [Fact]
    public void A_blank_co_author_username_is_required()
    {
        Validate(ValidTopic with { Authors = ["anna", "  "] })
            .ShouldHaveValidationErrorFor("authors[1].username").WithErrorMessage("Username is required.").Only();
    }

    [Fact]
    public void A_null_co_author_username_is_required()
    {
        Validate(ValidTopic with { Authors = [new AuthorData(null)] })
            .ShouldHaveValidationErrorFor("authors[0].username").WithErrorMessage("Username is required.").Only();
    }

    [Fact]
    public void A_missing_co_author_is_an_error_and_the_others_are_still_checked()
    {
        TestValidationResult<TRequest> result = Validate(ValidTopic with { Authors = ["anna", null, "anna"] });

        result.ShouldHaveValidationErrorFor("authors[1]").WithErrorMessage("This co-author is missing.");
        result.ShouldHaveValidationErrorFor("authors[2].username").WithErrorMessage("This author appears more than once.");
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void A_co_author_listed_again_in_another_case_appears_more_than_once()
    {
        Validate(ValidTopic with { Authors = ["anna", "bob", "ANNA"] })
            .ShouldHaveValidationErrorFor("authors[2].username")
            .WithErrorMessage("This author appears more than once.")
            .Only();
    }

    [Fact]
    public void Every_repeat_of_a_co_author_is_reported()
    {
        TestValidationResult<TRequest> result = Validate(ValidTopic with { Authors = ["bob", "bob", "anna", "Bob"] });

        result.ShouldHaveValidationErrorFor("authors[1].username").WithErrorMessage("This author appears more than once.");
        result.ShouldHaveValidationErrorFor("authors[3].username").WithErrorMessage("This author appears more than once.");
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Blank_co_author_usernames_are_each_required_rather_than_repeated()
    {
        TestValidationResult<TRequest> result = Validate(ValidTopic with { Authors = ["", ""] });

        result.ShouldHaveValidationErrorFor("authors[0].username").WithErrorMessage("Username is required.");
        result.ShouldHaveValidationErrorFor("authors[1].username").WithErrorMessage("Username is required.");
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Errors_on_different_fields_are_all_reported()
    {
        TestValidationResult<TRequest> result = Validate(new TopicData("", null, [], ["anna", "anna"]));

        result.ShouldHaveValidationErrorFor("title");
        result.ShouldHaveValidationErrorFor("questions");
        result.ShouldHaveValidationErrorFor("authors[1].username");
        Assert.Equal(3, result.Errors.Count);
    }

    private static AuthorData[] Usernames(int count) =>
        [.. Enumerable.Range(1, count).Select(i => new AuthorData($"author{i}"))];
}
