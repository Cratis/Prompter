// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Protection;
using NetCord;

namespace Cratis.Prompter.Discord;

/// <summary>Applies channel and thread-parent scope before any interaction work.</summary>
public static class ChannelAdmission
{
    /// <summary>Checks trusted channel metadata without consuming an admission slot.</summary>
    /// <param name="admission">The shared admission guard.</param>
    /// <param name="actor">The requesting member and location.</param>
    /// <param name="channel">The actual interaction channel, not a copied nullable parent ID.</param>
    /// <returns>Whether the channel and any required thread parent are known and in scope.</returns>
    public static bool InScope(Admission admission, DiscordActor actor, TextChannel? channel) =>
        channel is not null and not IUnknownTextChannel && channel.Id != 0 && channel.Id == actor.ChannelId &&
        admission.InScope(actor.GuildId, channel.Id) &&
        (channel is not GuildThread thread || (thread.ParentId != 0 && admission.InScope(actor.GuildId, thread.ParentId)));

    /// <summary>Admits work only after checking the channel and any required thread parent.</summary>
    /// <param name="admission">The shared admission guard.</param>
    /// <param name="actor">The requesting member and location.</param>
    /// <param name="channel">The actual interaction channel; missing metadata is rejected.</param>
    /// <param name="question">The transient admission key.</param>
    /// <returns>The admission decision, silent for missing or excluded channel scope.</returns>
    public static AdmissionDecision TryEnter(Admission admission, DiscordActor actor, TextChannel? channel, Question question) =>
        InScope(admission, actor, channel) ? admission.TryEnter(actor, question) : new(false, false);
}
