using System;
using System.IO;

namespace Cbo.Tests;

/// <summary>The repository's root directory, found from the test binaries.</summary>
internal static class RepoRoot
{
    public static string Path { get; } = Find();

    private static string Find()
    {
        for (DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (File.Exists(System.IO.Path.Combine(dir.FullName, "backend", "backend.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException(
            $"No directory above {AppContext.BaseDirectory} contains backend/backend.slnx.");
    }
}
