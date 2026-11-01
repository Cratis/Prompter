// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NetCord;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ComponentInteractions;

namespace Cratis.Prompter.Discord;

/// <summary>Provides a friendly, private reply for buttons the dispatcher no longer recognizes.</summary>
public class ButtonResults : ComponentInteractionResultHandler<ButtonInteractionContext>
{
    /// <summary>The reply for an unknown or expired button.</summary>
    public const string Expired = "This button has expired. Ask your question again and I'll help.";

    /// <inheritdoc/>
    public override ValueTask<InteractionMessageProperties> GetFailMessageAsync(IFailResult failResult, ButtonInteractionContext context, IServiceProvider services) =>
        failResult is NotFoundResult
            ? ValueTask.FromResult(new InteractionMessageProperties { Content = Expired, Flags = MessageFlags.Ephemeral })
            : base.GetFailMessageAsync(failResult, context, services);
}
