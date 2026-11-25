using System.Collections.Generic;
using System.Linq;
using Cbo.Core.Validation;
using Cbo.Results;
using FluentValidation.Results;

namespace Cbo.Tests.Validation;

public class ValidationResultExtensionsTests
{
    [Fact]
    public void Each_failure_becomes_a_field_error_with_its_path_and_message_in_the_same_order()
    {
        var result = new ValidationResult(
        [
            new ValidationFailure("title", "The title is required."),
            new ValidationFailure("questions[1].text", "Question text is required."),
            new ValidationFailure("title", "The title can be at most 100 characters."),
        ]);

        List<FieldError> fieldErrors = result.ToFieldErrors();

        Assert.Equal(
            [
                ("title", "The title is required."),
                ("questions[1].text", "Question text is required."),
                ("title", "The title can be at most 100 characters."),
            ],
            fieldErrors.Select(e => (e.Field, e.Message)));
    }

    [Fact]
    public void A_valid_result_gives_no_field_errors()
    {
        Assert.Empty(new ValidationResult().ToFieldErrors());
    }
}
