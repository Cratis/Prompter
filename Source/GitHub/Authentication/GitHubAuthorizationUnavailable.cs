// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.GitHub.Authentication;

/// <summary>
/// A credential or destination could not be authorized. Contains no response or credential data.
/// </summary>
public sealed class GitHubAuthorizationUnavailable() : Exception("GitHub authorization is unavailable.");
