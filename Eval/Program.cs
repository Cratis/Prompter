// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Eval;
using Cratis.Prompter.Eval.Scoring;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.DependencyInjection;

var (goldenPath, resultsDir) = EvalWorkspace.Resolve(args);

if (!File.Exists(goldenPath))
{
    await Console.Error.WriteLineAsync($"Golden question set not found at '{goldenPath}'. Pass --golden <path>.");
    return 1;
}

using var host = EvalHost.Build(args);

// Applies any pending schema migrations, exactly as the bot does at startup.
await host.Services.GetRequiredService<IChunks>().EnsureSchema();

var goldenSet = GoldenSet.Parse(await File.ReadAllTextAsync(goldenPath));
var questions = goldenSet.Questions.ToArray();
Console.WriteLine($"Loaded {questions.Length} golden questions from {goldenPath}");

var run = new EvaluationRun(
    host.Services.GetRequiredService<IAnswers>(),
    new GroundednessEvaluator(),
    new ChatConfiguration(host.Services.GetRequiredService<IChatClient>()));

var results = new List<QuestionResult>(questions.Length);
foreach (var question in questions)
{
    var result = await run.Score(question);
    results.Add(result);
    Console.WriteLine(result.ConsoleLine());
}

var report = new EvalReport(results, DateTimeOffset.UtcNow);
var stamp = report.GeneratedAt.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

Directory.CreateDirectory(resultsDir);
var markdownPath = Path.Combine(resultsDir, $"report-{stamp}.md");
var jsonPath = Path.Combine(resultsDir, $"report-{stamp}.json");

await File.WriteAllTextAsync(markdownPath, report.ToMarkdown());
await File.WriteAllTextAsync(jsonPath, report.ToJson());

Console.WriteLine();
Console.WriteLine(report.ConsoleSummary());
Console.WriteLine($"Wrote {markdownPath}");
Console.WriteLine($"Wrote {jsonPath}");

return 0;
