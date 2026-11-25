using System;
using System.Linq;
using Cbo.Core.Validation;
using FluentValidation;

namespace Cbo.Tests.Validation;

public class ValidatorBaseClassTests
{
    // Only the two bases check every item of a list, see ChildValidator<T>
    [Fact]
    public void Every_validator_in_core_derives_from_RequestValidator_or_ChildValidator()
    {
        string[] offenders =
        [
            .. typeof(RequestValidator<>).Assembly.GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IValidator).IsAssignableFrom(type))
                .Where(type => !DerivesFrom(type, typeof(RequestValidator<>)) && !DerivesFrom(type, typeof(ChildValidator<>)))
                .Select(type => type.FullName!),
        ];

        Assert.Empty(offenders);
    }

    private static bool DerivesFrom(Type type, Type genericBase)
    {
        for (Type? current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == genericBase)
                return true;
        }

        return false;
    }
}
