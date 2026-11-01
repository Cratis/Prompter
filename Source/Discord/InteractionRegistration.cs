// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using NetCord;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services.ComponentInteractions;

namespace Cratis.Prompter.Discord;

/// <summary>Registers the interaction services used by the Discord modules.</summary>
public static class InteractionRegistration
{
    /// <summary>Registers application commands and the single button dispatcher used by every button module.</summary>
    /// <param name="services">The host services.</param>
    /// <returns>The services for further registration.</returns>
    public static IServiceCollection AddPrompterInteractions(this IServiceCollection services) =>
        services.AddApplicationCommands().AddComponentInteractions<ButtonInteraction, ButtonInteractionContext>();
}
