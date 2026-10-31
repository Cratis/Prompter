// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Deployment.Services;

/// <summary>A configured Discord identifier is not a number.</summary>
public sealed class InvalidDiscordId() : Exception("A configured Discord id is not a valid number.");
