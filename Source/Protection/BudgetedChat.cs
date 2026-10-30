// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Protection;

/// <summary>Bounds every chat dispatch against its prepaid lease. SDK retries must be disabled.</summary>
/// <param name="inner">The provider with automatic retries disabled.</param>
/// <param name="configuration">Verified prices and hard model limits.</param>
public sealed class BudgetedChat(IChatClient inner, IOptions<PrompterOptions> configuration) : DelegatingChatClient(inner)
{
    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var config = configuration.Value;
        var budget = config.Protection.Anthropic;
        var lease = WorkLease.Current ?? throw new WorkRefused();
        var input = messages.ToArray();
        CheckModel(options);
        CheckInput(input, budget.MaxInputTokens);
        var bounded = BoundOutput(options, config.Answering.MaxOutputTokens);

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lease.Token);
        lease.DispatchChat();
        var result = await base.GetResponseAsync(input, bounded, linked.Token);
        if (result.Usage is { InputTokenCount: >= 0, OutputTokenCount: >= 0 } usage && NoAdditionalUsage(usage))
        {
            lease.ChatUsed(budget.Cost(usage.InputTokenCount.Value, usage.OutputTokenCount.Value));
        }

        return result;
    }

    /// <inheritdoc/>
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) => throw new WorkRefused();

    static bool NoAdditionalUsage(UsageDetails usage) =>
        usage.AdditionalCounts?.Values.All(count => count == 0) != false;

    static void CheckInput(ChatMessage[] input, int maximum)
    {
        if (input.Length > 4 || input.Any(message => message.Contents.Any(content => content is not TextContent)))
        {
            throw new WorkRefused();
        }

        if (input.Sum(message => (long)Encoding.UTF8.GetByteCount(message.Text)) + 1024 > maximum)
        {
            throw new WorkRefused();
        }
    }

    static ChatOptions BoundOutput(ChatOptions? request, int maximum)
    {
        var bounded = request?.Clone() ?? new ChatOptions();
        bounded.MaxOutputTokens = Math.Min(bounded.MaxOutputTokens ?? maximum, maximum);
        if (bounded.MaxOutputTokens <= 0)
        {
            throw new WorkRefused();
        }

        return bounded;
    }

    void CheckModel(ChatOptions? request)
    {
        var config = configuration.Value;
        if (!config.Protection.AnsweringEnabled || !config.Protection.Anthropic.Covers(config.Anthropic.Model, true))
        {
            throw new WorkRefused();
        }

        if (request?.Tools is { Count: > 0 } || !string.Equals(request?.ModelId ?? config.Anthropic.Model, config.Anthropic.Model, StringComparison.Ordinal))
        {
            throw new WorkRefused();
        }
    }
}
