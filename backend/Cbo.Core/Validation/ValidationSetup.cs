using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Internal;

namespace Cbo.Core.Validation;

/// <summary>
/// The FluentValidation options every validator relies on. <see cref="ServiceCollectionExtensions.AddCore"/> applies
/// them for the API and a module initializer applies them for the tests.
/// </summary>
public static class ValidationSetup
{
    /// <summary>
    /// Stops checking a property at its first failure (other properties are still checked) and names fields by their
    /// camelCase JSON path, so <c>Questions[0].Text</c> becomes <c>questions[0].text</c>. Safe to call more than once.
    /// </summary>
    public static void ConfigureGlobalOptions()
    {
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;
        ValidatorOptions.Global.PropertyNameResolver = ResolveJsonPath;
        ValidatorOptions.Global.DisplayNameResolver = ResolveJsonPath;
    }

    // FluentValidation joins these per-member names into the full path, adding the [i] indexes itself
    private static string? ResolveJsonPath(Type type, MemberInfo? memberInfo, LambdaExpression? expression)
    {
        string? path = null;

        if (expression is not null)
        {
            PropertyChain chain = PropertyChain.FromExpression(expression);
            if (chain.Count > 0)
                path = chain.ToString();
        }

        path ??= memberInfo?.Name;

        return path is null ? null : ToCamelCasePath(path);
    }

    private static string ToCamelCasePath(string path) =>
        string.Join(
            ValidatorOptions.Global.PropertyChainSeparator,
            path.Split(ValidatorOptions.Global.PropertyChainSeparator).Select(ToCamelCaseSegment));

    // Keeps an index suffix such as [0] as it is: Questions[0] becomes questions[0]
    private static string ToCamelCaseSegment(string segment)
    {
        int indexStart = segment.IndexOf('[');
        string name = indexStart < 0 ? segment : segment[..indexStart];
        string index = indexStart < 0 ? "" : segment[indexStart..];

        return JsonNamingPolicy.CamelCase.ConvertName(name) + index;
    }
}
