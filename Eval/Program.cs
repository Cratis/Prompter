// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Eval;

// No default paid mode, CLI configuration binding, appsettings or user-secrets.
if (!args.Contains("--offline", StringComparer.Ordinal) || args.Contains("--index", StringComparer.Ordinal) || args.Contains("--paid", StringComparer.Ordinal))
{
    await Console.Error.WriteLineAsync(EvalHost.Configuration().PaidBlockReason);
    return 2;
}

for (var index = 0; index < args.Length; index++)
{
    if (args[index] == "--offline") continue;
    if ((args[index] == "--out" || args[index] == "--revision") && index + 1 < args.Length &&
        !args[index + 1].StartsWith("--", StringComparison.Ordinal))
    {
        index++;
        continue;
    }
    await Console.Error.WriteLineAsync("Supported offline arguments: --offline --out <directory> --revision <source revision>.");
    return 2;
}

string? Value(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

var revision = Value("--revision");
if (string.IsNullOrWhiteSpace(revision) || revision.StartsWith("--", StringComparison.Ordinal))
{
    await Console.Error.WriteLineAsync("An explicit non-secret --revision is required for report provenance.");
    return 2;
}

var (_, resultsDir) = EvalWorkspace.Resolve(args);
var report = OfflineEvaluation.Run(revision);
Directory.CreateDirectory(resultsDir);
await File.WriteAllTextAsync(Path.Combine(resultsDir, "report-offline.json"), report.ToJson());
await File.WriteAllTextAsync(Path.Combine(resultsDir, "report-offline.md"), "Synthetic offline evidence — not measured model quality.\n\n" + report.ToMarkdown());
Console.WriteLine("Offline synthetic scoring complete; paid baseline remains unmeasured.");
return report.ExitCode;
