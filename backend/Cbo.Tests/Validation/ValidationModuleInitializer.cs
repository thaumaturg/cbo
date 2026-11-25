using System.Runtime.CompilerServices;
using Cbo.Core.Validation;

namespace Cbo.Tests.Validation;

/// <summary>
/// Applies the production FluentValidation options before any test runs, so validator tests get the same field paths
/// and cascade behavior as the API.
/// </summary>
internal static class ValidationModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize() => ValidationSetup.ConfigureGlobalOptions();
}
