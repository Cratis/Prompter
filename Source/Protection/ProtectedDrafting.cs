// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Protection;

/// <summary>Protects routing and drafting, including direct non-Discord callers.</summary>
/// <param name="inner">The drafting pipeline.</param>
/// <param name="work">The shared paid-work admission.</param>
/// <param name="options">The current switches and input limit.</param>
public sealed class ProtectedDrafting(IssueDrafting inner, PaidWork work, IOptions<PrompterOptions> options) : IIssueDrafting
{
    /// <inheritdoc/>
    public async Task<IssueDraft?> Draft(string conversation, string? hint, CancellationToken cancellationToken = default)
    {
        if ((long)conversation.Length + (hint?.Length ?? 0) > options.Value.Protection.MaxQuestionCharacters)
        {
            throw new WorkRefused();
        }

        if (!options.Value.Protection.AnsweringEnabled || !options.Value.Protection.GitHubWritesEnabled)
        {
            throw new WorkRefused();
        }

        if (WorkLease.Current is { } active)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, active.Token);
            return await inner.Draft(conversation, hint, linked.Token);
        }

        await using var lease = await work.BeginInteractive(cancellationToken);
        lease.Activate();
        return await inner.Draft(conversation, hint, lease.Token);
    }
}
