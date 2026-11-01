// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Protection;
using NetCord;

namespace Cratis.Prompter.Discord;

/// <summary>Applies parent-channel scope before consuming interactive admission.</summary>
public static class ChannelAdmission
{
    /// <summary>Admits a question only if both its channel and any thread parent are in scope.</summary>
    /// <param name="admission">The shared admission guard.</param>
    /// <param name="actor">The requesting member and location.</param>
    /// <param name="channel">The interaction channel.</param>
    /// <param name="question">The transient question.</param>
    /// <returns>The admission decision, silent for an excluded parent.</returns>
    public static AdmissionDecision TryEnter(Admission admission, DiscordActor actor, TextChannel channel, Question question) =>
        TryEnter(admission, actor, channel is GuildThread thread ? thread.ParentId : null, question);

    /// <summary>Admits an action only if its channel and trusted interaction parent are in scope.</summary>
    /// <param name="admission">The shared admission guard.</param>
    /// <param name="actor">The requesting member and location.</param>
    /// <param name="parentChannelId">The parent from the interaction channel, or null for a non-thread.</param>
    /// <param name="question">The transient admission key.</param>
    /// <returns>The admission decision, silent for an excluded parent.</returns>
    public static AdmissionDecision TryEnter(Admission admission, DiscordActor actor, ulong? parentChannelId, Question question) =>
        parentChannelId is { } parent && !admission.InScope(actor.GuildId, parent)
            ? new(false, false)
            : admission.TryEnter(actor, question);
}
