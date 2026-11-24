using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Cbo.Core.Domain;
using Cbo.Core.Paging;

namespace Cbo.Tests.Domain;

/// <summary>
/// Generates frontend/src/generated/domain-limits.json from the backend constants and fails when the committed file is
/// stale; see docs/specs/0001-shared-domain-limits.md.
/// </summary>
public class DomainLimitsExportTests
{
    private const string ExportPath = "frontend/src/generated/domain-limits.json";

    private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        IndentSize = 2,
        NewLine = "\n",
    };

    [Fact]
    public void Committed_domain_limits_match_the_backend_constants()
    {
        string path = Path.Combine(FindRepoRoot(), ExportPath);
        string expected = JsonSerializer.Serialize(BuildDocument(), s_jsonOptions) + "\n";
        string? actual = File.Exists(path) ? File.ReadAllText(path) : null;

        if (actual == expected)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, expected);
        Assert.Fail($"{ExportPath} was regenerated. Review the diff and commit it.");
    }

    private static SortedDictionary<string, object> BuildDocument()
    {
        var limits = new SortedDictionary<string, object>(StringComparer.Ordinal);
        foreach (Type area in typeof(Limits).GetNestedTypes(BindingFlags.Public))
        {
            // Static classes compile to abstract sealed types.
            if (!area.IsAbstract || !area.IsSealed)
            {
                continue;
            }

            limits[ToCamelCase(area.Name)] = ReadConstAndReadonlyFields(area);
        }

        return new SortedDictionary<string, object>(StringComparer.Ordinal)
        {
            ["limits"] = limits,
            ["paging"] = ReadConstAndReadonlyFields(typeof(PagingDefaults)),
            ["tournamentDefaults"] = ReadConstAndReadonlyFields(typeof(TournamentDefaults)),
        };
    }

    private static SortedDictionary<string, object> ReadConstAndReadonlyFields(Type type)
    {
        var values = new SortedDictionary<string, object>(StringComparer.Ordinal);
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.IsLiteral || field.IsInitOnly)
            {
                values[ToCamelCase(field.Name)] = field.GetValue(null)!;
            }
        }

        return values;
    }

    private static string ToCamelCase(string name) => JsonNamingPolicy.CamelCase.ConvertName(name);

    private static string FindRepoRoot()
    {
        for (DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "backend", "backend.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException(
            $"No directory above {AppContext.BaseDirectory} contains backend/backend.slnx.");
    }
}
