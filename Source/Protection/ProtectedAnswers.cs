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
        if (question.Value.Length > options.Value.Protection.MaxQuestionCharacters || !options.Value.Protection.AnsweringEnabled)
        {
            throw new WorkRefused();
        }

        if (WorkLease.Current is { } active)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, active.Token);
            return await inner.For(question, source, linked.Token);
        }

        await using var lease = await work.BeginInteractive(cancellationToken);
        lease.Activate();
        return await inner.For(question, source, lease.Token);
    }
}
