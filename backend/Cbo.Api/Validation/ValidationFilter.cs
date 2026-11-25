using System.Collections.Concurrent;
using Cbo.Api.ErrorHandling;
using Cbo.Core.Validation;
using Cbo.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Cbo.Api.Validation;

/// <summary>
/// Validates every endpoint argument that has a registered <see cref="IValidator{T}"/> (JSON bodies, bare-list bodies
/// and <c>[AsParameters]</c> objects) before the handler runs, and answers with a 400 problem when one is invalid.
/// Arguments without a validator pass through. See
/// docs/adr/0011-request-validation-with-fluentvalidation-before-access-checks.md.
/// </summary>
public sealed class ValidationFilter : IEndpointFilter
{
    private static readonly ConcurrentDictionary<Type, Type> s_validatorTypes = new();

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        IServiceProvider services = context.HttpContext.RequestServices;

        foreach (object? argument in context.Arguments)
        {
            if (argument is null)
                continue;

            Type validatorType = s_validatorTypes.GetOrAdd(argument.GetType(), type => typeof(IValidator<>).MakeGenericType(type));
            if (services.GetService(validatorType) is not IValidator validator)
                continue;

            ValidationResult result = await validator.ValidateAsync(
                new ValidationContext<object>(argument), context.HttpContext.RequestAborted);

            if (!result.IsValid)
                return Failure.Invalid(InvalidMessageOf(validator), result.ToFieldErrors()).ToProblem();
        }

        return await next(context);
    }

    private static string InvalidMessageOf(IValidator validator) =>
        validator is IRequestValidator requestValidator
            ? requestValidator.InvalidMessage
            : throw new InvalidOperationException(
                $"{validator.GetType().Name} validates a request, so it must derive from {nameof(RequestValidator<>)}<T>.");
}
