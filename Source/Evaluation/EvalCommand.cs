// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Cli;
using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Evaluation;

/// <summary>Runs paid cases sequentially through the same protection as interactive requests.</summary>
/// <param name="work">The shared admission and durable spend gate.</param>
/// <param name="answers">The protected answer pipeline.</param>
public sealed class EvalCommand(PaidWork work, IAnswers answers)
{
    static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    /// <summary>Runs a selected set, stopping between cases at the cost cap.</summary>
    /// <param name="set">The curated set.</param>
    /// <param name="arguments">The explicit paid opt-in and limits.</param>
    /// <returns>The quality and spend summary.</returns>
    public async Task<EvalSummary> Run(EvalSet set, EvalArguments arguments)
    {
        if (!arguments.Paid) return EvalSummary.From([], arguments);
        var selected = set.Questions.Where(item => arguments.Case is null || item.Id == arguments.Case)
            .Take(arguments.Limit ?? int.MaxValue);
        var results = new List<EvalResult>();
        var stopped = false;
        foreach (var item in selected)
        {
            if (results.Sum(result => result.Cost) >= arguments.MaxCost)
            {
                stopped = true;
                break;
            }

            results.Add(await RunCase(item));
        }

        var summary = EvalSummary.From(results, arguments, stopped);
        if (arguments.Out is { } path)
        {
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(summary, _jsonOptions));
        }

        return summary;
    }

    async Task<EvalResult> RunCase(EvalCase item)
    {
        WorkLease? lease = null;
        try
        {
            lease = await work.BeginInteractive();
            await using var completion = lease;
            lease.Activate();
            var answer = await answers.For(new(item.Question), "eval", lease.Token);
            return EvalScoring.Score(item, answer, lease.Spent);
        }
        catch (WorkRefused)
        {
            return new(item.Id, item.Expected == "answer", false, false, false, false, false, lease?.Spent ?? 0, [], "refused by protection");
        }
    }
}
