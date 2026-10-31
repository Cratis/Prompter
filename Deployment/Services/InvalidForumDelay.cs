// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Deployment.Services;

/// <summary>A configured forum delay is not a whole number of minutes from 1 to 1440.</summary>
public sealed class InvalidForumDelay() : Exception("A configured forum delay must be a whole number of minutes from 1 to 1440.");
