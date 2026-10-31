// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Deployment.Services;

/// <summary>The explicit installation identity or key reference is incomplete or invalid.</summary>
public sealed class InvalidGitHubApp() : Exception("GitHub App deployment configuration is not approved.");
