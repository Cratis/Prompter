// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.GitHub.Authentication;

/// <summary>
/// An owner/repository pair authorized as one destination, never as independent route fragments.
/// </summary>
/// <param name="Owner">The repository owner.</param>
/// <param name="Repository">The repository name.</param>
public sealed record GitHubDestination(string Owner, string Repository);
