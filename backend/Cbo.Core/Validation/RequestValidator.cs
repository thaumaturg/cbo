using System.Linq.Expressions;
using FluentValidation;

namespace Cbo.Core.Validation;

/// <summary>
/// The validator for a request DTO. It checks only what the request itself shows; rules that need the database, the
/// route, the current user or a tournament's settings stay in the handler. Registered validators run before the
/// handler; see docs/adr/0011-request-validation-with-fluentvalidation-before-access-checks.md.
/// Objects nested in the request use a <see cref="ChildValidator{T}"/>.
/// </summary>
public abstract class RequestValidator<T> : AbstractValidator<T>, IRequestValidator
{
    public abstract string InvalidMessage { get; }

    /// <inheritdoc cref="ChildValidator{T}.RuleForEach{TElement}"/>
    public new IRuleBuilderInitialCollection<T, TElement> RuleForEach<TElement>(
        Expression<Func<T, IEnumerable<TElement>>> expression) =>
        base.RuleForEach(expression).Cascade(CascadeMode.Continue);
}
