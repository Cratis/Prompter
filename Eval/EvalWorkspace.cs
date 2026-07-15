// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Eval;

/// <summary>
/// Resolves where the harness reads the golden set and writes its reports, so
/// <c>dotnet run --project Eval</c> works regardless of the current directory.
/// </summary>
internal static class EvalWorkspace
{
    const string GoldenFileName = "golden-questions.yaml";
    const string ResultsFolder = "results";
    const string EvalFolder = "Eval";

    /// <summary>
    /// Resolves the golden-set path and the results directory. Both can be overridden with
    /// <c>--golden &lt;path&gt;</c> and <c>--out &lt;dir&gt;</c>; otherwise the harness walks up from the
    /// running assembly to locate the <c>Eval</c> directory.
    /// </summary>
    /// <param name="args">The process arguments.</param>
    /// <returns>The resolved golden-set path and results directory.</returns>
    public static (string GoldenPath, string ResultsDir) Resolve(IReadOnlyList<string> args)
    {
        var golden = ArgValue(args, "--golden");
        var resultsDir = ArgValue(args, "--out");

        if (golden is not null && resultsDir is not null)
        {
            return (golden, resultsDir);
        }

        var located = LocateEvalDirectory() ?? AppContext.BaseDirectory;
        golden ??= Path.Combine(located, GoldenFileName);
        resultsDir ??= Path.Combine(located, ResultsFolder);

        return (golden, resultsDir);
    }

    static string? LocateEvalDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        for (var depth = 0; directory is not null && depth < 8; depth++, directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, GoldenFileName)))
            {
                return directory.FullName;
            }

            var evalSubdirectory = Path.Combine(directory.FullName, EvalFolder);
            if (File.Exists(Path.Combine(evalSubdirectory, GoldenFileName)))
            {
                return evalSubdirectory;
            }
        }

        return null;
    }

    static string? ArgValue(IReadOnlyList<string> args, string name)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }
}
