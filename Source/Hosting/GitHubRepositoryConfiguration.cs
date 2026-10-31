// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Prompter.GitHub.Authentication;
using Microsoft.Extensions.Configuration;

namespace Cratis.Prompter.Hosting;

/// <summary>Resolves an authoritative installation allowlist after ordinary options binding.</summary>
public static class GitHubRepositoryConfiguration
{
    /// <summary>Replaces inherited indexes if the atomic scalar is present; absence preserves legacy binding.</summary>
    /// <param name="configuration">The layered configuration.</param>
    /// <param name="options">The bound options, before consumers snapshot authorization.</param>
    /// <exception cref="GitHubAuthorizationUnavailable">The atomic setting is not an array of strings.</exception>
    public static void Apply(IConfiguration configuration, PrompterOptions options)
    {
        var section = configuration.GetSection("Cratis:Prompter:GitHub:InstallationRepositoriesJson");
        if (!IsPresent(configuration, section)) return;
        options.GitHub.InstallationRepositories = [];
        try
        {
            using var document = JsonDocument.Parse(section.Value ?? string.Empty);
            if (section.GetChildren().Any() || document.RootElement.ValueKind != JsonValueKind.Array)
                throw new GitHubAuthorizationUnavailable();
            var repositories = new List<string>();
            foreach (var member in document.RootElement.EnumerateArray())
            {
                if (member.ValueKind != JsonValueKind.String) throw new GitHubAuthorizationUnavailable();
                repositories.Add(member.GetString()!);
            }
            options.GitHub.InstallationRepositories = repositories;
        }
        catch (JsonException)
        {
            throw new GitHubAuthorizationUnavailable();
        }
    }

    static bool IsPresent(IConfiguration configuration, IConfigurationSection section) =>
        section.Value is not null || section.GetChildren().Any()
        || configuration.GetSection("Cratis:Prompter:GitHub").GetChildren()
            .Any(child => child.Key.Equals("InstallationRepositoriesJson", StringComparison.OrdinalIgnoreCase));
}
