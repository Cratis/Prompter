// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Prompter.Deployment.Services;

/// <summary>Projects explicit App metadata without credentials or write activation.</summary>
public static class GitHubAppEnvironment
{
    /// <summary>The fixed absolute path of the projected PEM file.</summary>
    public const string KeyPath = "/var/run/prompter/github-app/private-key.pem";

    /// <summary>Creates authoritative installation settings; absent configuration remains inert.</summary>
    /// <param name="args">The complete installation identity.</param>
    /// <returns>Non-secret runtime settings.</returns>
    /// <exception cref="InvalidGitHubApp">The configuration is incomplete or invalid.</exception>
    public static IReadOnlyDictionary<string, string> Values(GitHubAppArgs args)
    {
        if (!args.IsValid) throw new InvalidGitHubApp();
        if (!args.IsConfigured) return new Dictionary<string, string>();
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Cratis__Prompter__GitHub__AppId"] = args.AppId,
            ["Cratis__Prompter__GitHub__InstallationId"] = args.InstallationId,
            ["Cratis__Prompter__GitHub__Owner"] = args.Owner,
            ["Cratis__Prompter__GitHub__InstallationRepositoriesJson"] = JsonSerializer.Serialize(args.Repositories.Split(',')),
            ["Cratis__Prompter__GitHub__ApiUrl"] = "https://api.github.com/",
            ["Cratis__Prompter__GitHub__AppPrivateKeyPath"] = KeyPath,
            ["Cratis__Prompter__GitHub__AllowTokenFallback"] = "false",
            ["Cratis__Prompter__Protection__GitHubWritesEnabled"] = "false"
        };
    }
}
