// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Protection;

/// <summary>Protects direct CLI/webhook callers as well as already-admitted Discord requests.</summary>
/// <param name="inner">The answer pipeline.</param>
/// <param name="work">The shared paid-work admission.</param>
/// <param name="options">The current input limit.</param>
public sealed class ProtectedAnswers(Answers inner, PaidWork work, IOptions<PrompterOptions> options) : IAnswers
{
    /// <inheritdoc/>
    public async Task<Answer> For(Question question, string source, CancellationToken cancellationToken = default)
    {
        var current = SecretRedaction.Redact(question.Value);
        var messages = question.Conversation.Messages.Select(message =>
            message with { Content = SecretRedaction.Redact(message.Content).Text }).ToArray();
        var redacted = current.Removed || current.Text.Contains(SecretRedaction.Marker, StringComparison.Ordinal) ||
            messages.Any(message => message.Content.Contains(SecretRedaction.Marker, StringComparison.Ordinal));
        question = question with { Value = current.Text, Conversation = question.Conversation with { Messages = messages } };
        if (question.Value.Length > options.Value.Protection.MaxQuestionCharacters || !options.Value.Protection.AnsweringEnabled)
        {
            throw new WorkRefused();
        }

        if (WorkLease.Current is { } active)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, active.Token);
            var answer = await inner.For(question, source, linked.Token);
            return answer with { SecretsRedacted = redacted };
        }

        await using var lease = await work.BeginInteractive(cancellationToken);
        lease.Activate();
        var result = await inner.For(question, source, lease.Token);
        return result with { SecretsRedacted = redacted };
    }
}
