using System.Linq.Expressions;
using FluentValidation;

namespace Cbo.Core.Validation;

/// <summary>
/// The validator for an object nested in a request, attached with <c>SetValidator</c>. Like
/// <see cref="RequestValidator{T}"/>, its <see cref="RuleForEach{TElement}"/> checks every item of a list.
/// </summary>
public abstract class ChildValidator<T> : AbstractValidator<T>
{
    /// <summary>
    /// <inheritdoc cref="AbstractValidator{T}.RuleForEach{TElement}"/> Every item is checked, even after one fails;
    /// with the global <see cref="CascadeMode.Stop"/>, FluentValidation would skip the rest of the list. Give each item a
    /// single check or a child validator, because all of an item's checks run.
    /// </summary>
    public new IRuleBuilderInitialCollection<T, TElement> RuleForEach<TElement>(
        Expression<Func<T, IEnumerable<TElement>>> expression) =>
        base.RuleForEach(expression).Cascade(CascadeMode.Continue);
}
