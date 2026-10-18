using System;
using System.Collections.Generic;
using Cbo.Results;

namespace Cbo.Tests.Results;

public class ResultOfTTests
{
    [Fact]
    public void Value_converts_implicitly_to_a_success_holding_that_value()
    {
        Result<string> result = "Round 3";

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("Round 3", result.Value);
        Assert.Throws<InvalidOperationException>(() => result.Failure);
    }

    [Fact]
    public void Success_factory_behaves_like_the_implicit_conversion_from_a_value()
    {
        Result<string> result = Result<string>.Success("Round 3");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("Round 3", result.Value);
        Assert.Throws<InvalidOperationException>(() => result.Failure);
    }

    [Fact]
    public void Success_factory_accepts_a_value_for_an_interface_typed_result()
    {
        string[] rounds = ["Round 1", "Round 2"];

        Result<IReadOnlyList<string>> result = Result<IReadOnlyList<string>>.Success(rounds);

        Assert.True(result.IsSuccess);
        Assert.Same(rounds, result.Value);
    }

    [Fact]
    public void A_success_cannot_hold_null()
    {
        string? missing = null;

        Assert.Throws<ArgumentNullException>(() => Result<string>.Success(missing!));
        Assert.Throws<ArgumentNullException>(() =>
        {
            Result<string> result = missing!;
        });
    }

    [Fact]
    public void A_null_failure_is_rejected()
    {
        Failure? missing = null;

        Assert.Throws<ArgumentNullException>(() =>
        {
            Result<string> result = missing!;
        });
    }

    [Fact]
    public void Failure_converts_implicitly_to_a_failed_result_and_reading_its_value_throws()
    {
        Result<string> result = Failure.Invalid(
            "The round is invalid.",
            new FieldError("answers", "The answers do not fit the scoring mode."));

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Equal(FailureKind.Invalid, result.Failure.Kind);
        Assert.Equal("The round is invalid.", result.Failure.Message);
        FieldError error = Assert.Single(result.Failure.FieldErrors);
        Assert.Equal("answers", error.Field);
        Assert.Equal("The answers do not fit the scoring mode.", error.Message);
    }

    [Fact]
    public void Failure_passed_on_to_a_result_of_another_type_keeps_kind_message_and_field_errors()
    {
        Result<string> original = Failure.Conflict(
            "The round already exists.",
            new FieldError("numberInMatch", "Round 3 is already recorded."));

        Result<int> passedOn = PassOn(original);

        Assert.True(passedOn.IsFailure);
        Assert.Equal(FailureKind.Conflict, passedOn.Failure.Kind);
        Assert.Equal("The round already exists.", passedOn.Failure.Message);
        FieldError error = Assert.Single(passedOn.Failure.FieldErrors);
        Assert.Equal("numberInMatch", error.Field);
        Assert.Equal("Round 3 is already recorded.", error.Message);
    }

    private static Result<int> PassOn(Result<string> result)
    {
        if (result.IsFailure)
        {
            return result.Failure;
        }

        return result.Value.Length;
    }
}
