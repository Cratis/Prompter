// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.Extensions.AI;

namespace Cratis.Prompter.Answering;

/// <summary>
/// Validates task-specific models and produces the same bounded request for either account.
/// </summary>
public static class TaskChatPolicy
{
    static readonly HashSet<string> _efforts = new(StringComparer.Ordinal) { string.Empty, "low", "medium", "high" };

    /// <summary>
    /// Selects a fixed task name without accepting arbitrary metric labels.
    /// </summary>
    /// <param name="options">The caller options.</param>
    /// <returns>The fixed task name.</returns>
    public static string TaskName(ChatOptions? options) =>
        options?.AdditionalProperties?.TryGetValue(AnthropicAccounts.TaskKey, out var value) == true && string.Equals(value as string, "issue-draft", StringComparison.Ordinal)
            ? "issue-draft" : "answer";

    /// <summary>
    /// Computes a conservative text-token ceiling and rejects unsupported multimodal requests.
    /// </summary>
    /// <param name="messages">The complete prompt.</param>
    /// <param name="limit">The task input ceiling.</param>
    /// <returns>The reservation upper bound.</returns>
    /// <exception cref="ProviderUnavailable">The prompt exceeds the policy or contains unsupported content.</exception>
    public static int InputBound(ChatMessage[] messages, int limit)
    {
        var bytes = messages.Sum(message => Encoding.UTF8.GetByteCount(message.Text) + 64);
        var unsupported = messages.Any(message => message.Contents.Any(content => content is not TextContent));
        if (unsupported || bytes > limit)
        {
            throw new ProviderUnavailable("prompt_limit");
        }

        return bytes;
    }

    /// <summary>
    /// Applies a validated task policy, without mutating caller options.
    /// </summary>
    /// <param name="configuration">The provider configuration.</param>
    /// <param name="policy">The task policy.</param>
    /// <param name="task">The fixed task name.</param>
    /// <param name="supplied">The caller options.</param>
    /// <returns>Bounded provider options.</returns>
    /// <exception cref="ProviderUnavailable">The request or configured policy is unsupported.</exception>
    public static ChatOptions Prepare(AnthropicOptions configuration, TaskModelOptions policy, string task, ChatOptions? supplied)
    {
        ValidateBounds(policy);
        var model = policy.Model ?? configuration.Model;
        ValidateModel(configuration, policy, model, task);
        var request = supplied?.Clone() ?? new ChatOptions();
        if (request.Tools?.Count > 0 || request.MaxOutputTokens < 1)
        {
            throw new ProviderUnavailable("unsupported_request");
        }

        request.ModelId = model;
        request.MaxOutputTokens = Math.Min(request.MaxOutputTokens ?? policy.MaxOutputTokens, policy.MaxOutputTokens);
        request.Reasoning = Reasoning(policy.Effort);
        request.Temperature = null;
        request.TopP = null;
        request.TopK = null;
        return request;
    }

    static ReasoningOptions? Reasoning(string effort) => effort.Length == 0 ? null : new()
    {
        Effort = effort switch
        {
            "low" => ReasoningEffort.Low,
            "medium" => ReasoningEffort.Medium,
            _ => ReasoningEffort.High
        }
    };

    static void ValidateModel(AnthropicOptions configuration, TaskModelOptions policy, string model, string task)
    {
        if (!model.Contains("haiku", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var approvedTask = task == "issue-draft" && configuration.HaikuIssueDraftApproved;
        var compatibleModel = model == "claude-haiku-5-5" && policy.Effort.Length == 0;
        if (!approvedTask || !compatibleModel)
        {
            throw new ProviderUnavailable("haiku_not_approved");
        }
    }

    static void ValidateBounds(TaskModelOptions policy)
    {
        ValidateRange(policy.MaxOutputTokens, 4096);
        ValidateRange(policy.MaxPromptTokens, 32_000);
        if (!_efforts.Contains(policy.Effort))
        {
            throw new ProviderUnavailable("invalid_task_policy");
        }
    }

    static void ValidateRange(int value, int ceiling)
    {
        if (value < 1 || value > ceiling)
        {
            throw new ProviderUnavailable("invalid_task_policy");
        }
    }
}
