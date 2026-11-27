using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cbo.Core.Domain;
using Cbo.Core.Paging;
using Cbo.Core.Playoffs;

namespace Cbo.Tests.Domain;

/// <summary>
/// Generates frontend/src/generated/domain-limits.json from the backend constants and fails when the committed file is
/// stale; see docs/adr/0010-domain-limits-live-in-code-and-ship-to-the-frontend-at-build-time.md.
/// </summary>
public class DomainLimitsExportTests
{
    private const string ExportPath = "frontend/src/generated/domain-limits.json";

    private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        IndentSize = 2,
        NewLine = "\n",
        // Enums by name, as the API sends them
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public void Committed_domain_limits_match_the_backend_constants()
    {
        string path = Path.Combine(RepoRoot.Path, ExportPath);
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
            ["playoffBrackets"] = PlayoffBrackets(),
            ["tournamentDefaults"] = ReadConstAndReadonlyFields(typeof(TournamentDefaults)),
        };
    }

    /// <summary>
    /// Each format's playoff bracket template: every playoff match's and the Final's seats, and where its winners and
    /// losers go. Formats without a bracket are left out.
    /// </summary>
    private static SortedDictionary<string, object> PlayoffBrackets()
    {
        var brackets = new SortedDictionary<string, object>(StringComparer.Ordinal);
        foreach (TournamentFormat format in Enum.GetValues<TournamentFormat>())
        {
            if (PlayoffBracket.For(format) is not PlayoffBracket bracket)
            {
                continue;
            }

            var matches = new SortedDictionary<string, object>(StringComparer.Ordinal);
            foreach ((BracketPosition position, ImmutableArray<BracketSlot> slots) in bracket.Matches)
            {
                BracketDestinations destinations = bracket.Destinations(position);
                matches[position.ToString()] = new SortedDictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["losersTo"] = destinations.Losers,
                    ["slots"] = slots.Select(Slot).ToList(),
                    ["winnersTo"] = destinations.Winners,
                };
            }

            brackets[format.ToString()] = matches;
        }

        return brackets;
    }

    private static SortedDictionary<string, object> Slot(BracketSlot slot)
    {
        var values = new SortedDictionary<string, object>(StringComparer.Ordinal) { ["label"] = slot.Label };
        switch (slot)
        {
            case QualificationPlaceSlot qualification:
                values["qualificationPlace"] = qualification.Place;
                break;
            case OutcomeSlot outcome:
                values["outcome"] = outcome.Outcome;
                values["source"] = outcome.Source;
                break;
        }

        return values;
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
}
