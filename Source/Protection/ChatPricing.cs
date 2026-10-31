// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Protection;

/// <summary>
/// Selects the verified price table, effective model and output ceiling for a fixed chat task, so
/// admission, durable accounting and metrics all agree. Prices never fall back to another model's table.
/// </summary>
public static class ChatPricing
{
    /// <summary>The fixed task name of issue drafting; every other chat task is an answer.</summary>
    public const string IssueDraftTask = "issue-draft";

    /// <summary>Selects the verified prices for a task.</summary>
    /// <param name="config">The Prompter options.</param>
    /// <param name="task">The fixed task name.</param>
    /// <returns>The task's price table.</returns>
    public static ProviderBudget ForTask(PrompterOptions config, string task) =>
        task == IssueDraftTask ? config.Protection.Anthropic : config.Protection.AnthropicAnswer;

    /// <summary>Resolves the effective model of a task.</summary>
    /// <param name="config">The Prompter options.</param>
    /// <param name="task">The fixed task name.</param>
    /// <returns>The model the task dispatches to.</returns>
    public static string Model(PrompterOptions config, string task) =>
        (task == IssueDraftTask ? config.Anthropic.IssueDraft : config.Anthropic.Answer).Model ?? config.Anthropic.Model;

    /// <summary>Resolves the output-token ceiling of a task.</summary>
    /// <param name="config">The Prompter options.</param>
    /// <param name="task">The fixed task name.</param>
    /// <returns>The maximum output tokens a dispatch may request.</returns>
    public static int OutputCeiling(PrompterOptions config, string task) =>
        task == IssueDraftTask
            ? config.Anthropic.IssueDraft.MaxOutputTokens
            : Math.Min(config.Anthropic.Answer.MaxOutputTokens, config.Answering.MaxOutputTokens);

    /// <summary>Checks that every chat task has verified prices for its effective model.</summary>
    /// <param name="config">The Prompter options.</param>
    /// <returns>Whether all chat tasks can be bounded.</returns>
    public static bool Covered(PrompterOptions config) =>
        ForTask(config, "answer").Covers(Model(config, "answer"), true) &&
        ForTask(config, IssueDraftTask).Covers(Model(config, IssueDraftTask), true);

    /// <summary>Computes the conservative whole-operation chat exposure: the larger of the two tasks.</summary>
    /// <param name="config">The Prompter options.</param>
    /// <returns>The dollar exposure to reserve before either task can run.</returns>
    public static decimal MaximumExposure(PrompterOptions config) => Math.Max(
        Exposure(config, "answer"),
        Exposure(config, IssueDraftTask));

    static decimal Exposure(PrompterOptions config, string task)
    {
        var budget = ForTask(config, task);
        return budget.Cost(budget.MaxInputTokens, OutputCeiling(config, task));
    }
}
