// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;

namespace Cratis.Prompter.Answering;

/// <summary>
/// One bounded nonstreaming generation operation across authorized accounts. Secondary use requires
/// both task opt-in and accounting reservations. Unknown failures never authorize account hopping.
/// </summary>
/// <param name="primary">The primary client, with SDK retries disabled.</param>
/// <param name="secondary">The optional secondary client, with SDK retries disabled.</param>
/// <param name="configuration">Task and account configuration.</param>
/// <param name="accounting">Shared durable reservation/accounting integration.</param>
public sealed class AnthropicAccounts(
    IChatClient primary,
    IChatClient? secondary,
    AnthropicOptions configuration,
    IProviderAccounting? accounting = null) : DelegatingChatClient(primary)
{
    /// <summary>
    /// The AdditionalProperties key selecting the fixed issue-draft task; absent means answer.
    /// </summary>
    public const string TaskKey = "PrompterTask";

    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var task = TaskChatPolicy.TaskName(options);
        var policy = task == "answer" ? configuration.Answer : configuration.IssueDraft;
        var request = TaskChatPolicy.Prepare(configuration, policy, task, options);
        var input = messages.ToArray();
        var bytes = TaskChatPolicy.InputBound(input, policy.MaxPromptTokens);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(policy.TimeoutSeconds, 1, 60)));
        var operation = Guid.NewGuid();
        var (client, account) = SelectAccount(policy);
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            timeout.Token.ThrowIfCancellationRequested();
            var dispatch = new ProviderDispatch(operation, "anthropic", account, task, attempt, bytes, request.MaxOutputTokens!.Value, request.ModelId!, policy.Effort);
            try
            {
                return await Send(client, (input, request), dispatch, timeout.Token);
            }
            catch (AnthropicCapacityFailure failure) when (attempt == 1)
            {
                await Task.Delay(failure.RetryAfter ?? TimeSpan.FromMilliseconds(Random.Shared.Next(100, 1000)), timeout.Token);
            }
            catch (AnthropicCapacityFailure)
            {
                throw new ProviderUnavailable("capacity_exhausted");
            }
        }

        throw new ProviderUnavailable("capacity_exhausted");
    }

    /// <inheritdoc/>
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) => throw new ProviderUnavailable("streaming_not_supported");

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            secondary?.Dispose();
        }

        base.Dispose(disposing);
    }

    static string Finish(ChatResponse? response) => response?.FinishReason switch
    {
        null => "unknown",
        var reason when reason == ChatFinishReason.Stop => "stop",
        var reason when reason == ChatFinishReason.Length => "length",
        var reason when reason == ChatFinishReason.ContentFilter => "refusal",
        _ => "other"
    };

    (IChatClient Client, string Account) SelectAccount(TaskModelOptions policy)
    {
        // Only a provider-confirmed monthly allocation reset is currently accepted as account-scoped
        // evidence. Generic 429/503/529, text messages, auth and billing errors cannot switch identities.
        if (!(configuration.PrimaryQuotaResetAt > DateTimeOffset.UtcNow))
        {
            return (InnerClient, "primary");
        }

        var authorized = policy.AllowSecondary && accounting is not null;
        if (!authorized || secondary is null)
        {
            throw new ProviderUnavailable("quota_exhausted");
        }

        return (secondary, "secondary");
    }

    async Task<ChatResponse> Send(IChatClient client, (ChatMessage[] Messages, ChatOptions Options) request, ProviderDispatch dispatch, CancellationToken token)
    {
        if (accounting is not null && !await accounting.Reserve(dispatch, token))
        {
            throw new ProviderUnavailable("budget_unavailable");
        }

        ChatResponse? response = null;
        try
        {
            response = await client.GetResponseAsync(request.Messages, request.Options, token);
            return response;
        }
        catch (AnthropicCapacityFailure)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new ProviderUnavailable("provider_failure");
        }
        finally
        {
            if (accounting is not null)
            {
                await accounting.Complete(dispatch, response?.Usage, Finish(response));
            }
        }
    }
}
