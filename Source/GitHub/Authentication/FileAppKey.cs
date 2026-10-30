// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Prompter.GitHub.Authentication;

/// <summary>
/// Reads only the explicitly configured, deployment-mounted PEM file.
/// </summary>
/// <param name="options">The configuration naming the mounted key file.</param>
public sealed class FileAppKey(IOptions<PrompterOptions> options) : IAppKey
{
    readonly string _path = options.Value.GitHub.AppPrivateKeyPath;

    /// <inheritdoc/>
    public async Task<string> Read(CancellationToken cancellationToken)
    {
        try
        {
            var file = new FileInfo(_path);
            if (file.Length is <= 0 or > 16384)
            {
                throw new GitHubAuthorizationUnavailable();
            }
            return await File.ReadAllTextAsync(_path, cancellationToken);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        {
            throw new GitHubAuthorizationUnavailable();
        }
    }
}
