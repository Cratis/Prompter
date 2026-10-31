// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.GitHub.Authentication;

/// <summary>
/// Immutable deployment authorization, independent of mutable preview-time options.
/// </summary>
/// <param name="options">The deployment configuration to pin.</param>
public sealed class GitHubPolicy(IOptions<PrompterOptions> options)
{
    readonly string _api = options.Value.GitHub.ApiUrl;
    readonly string _owner = options.Value.GitHub.Owner;
    readonly ImmutableHashSet<string> _repositories = options.Value.GitHub.InstallationRepositories.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
    readonly bool _fallbackAllowed = options.Value.GitHub.AllowTokenFallback;
    readonly string _fallback = options.Value.GitHub.Token;

    /// <summary>
    /// Gets the App ID pinned at construction.
    /// </summary>
    public long AppId { get; } = options.Value.GitHub.AppId;

    /// <summary>
    /// Gets the installation ID pinned at construction.
    /// </summary>
    public long InstallationId { get; } = options.Value.GitHub.InstallationId;

    /// <summary>
    /// Gets whether any part of App configuration is present; partial configuration never falls back.
    /// </summary>
    public bool HasApp { get; } = options.Value.GitHub.AppId != 0 || options.Value.GitHub.InstallationId != 0 || !string.IsNullOrWhiteSpace(options.Value.GitHub.AppPrivateKeyPath);

    /// <summary>
    /// Gets the names requested from the single approved owner installation.
    /// </summary>
    public ImmutableArray<string> RepositoryNames => _repositories.Order(StringComparer.OrdinalIgnoreCase).Select(repository => repository.Split('/')[1]).ToImmutableArray();

    /// <summary>
    /// Validates the fixed public GitHub API and explicit installation allowlist.
    /// </summary>
    /// <exception cref="GitHubAuthorizationUnavailable">The API or installation allowlist is invalid.</exception>
    public void Validate()
    {
        if (!ValidConfiguration())
        {
            throw new GitHubAuthorizationUnavailable();
        }
    }

    /// <summary>
    /// Requires the exact owner/repository to be in the pinned deployment allowlist.
    /// </summary>
    /// <param name="destination">The owner and repository from the immutable payload or parsed request.</param>
    /// <exception cref="GitHubAuthorizationUnavailable">The destination is not approved.</exception>
    public void RequireRepository(GitHubDestination destination)
    {
        Validate();
        if (!ApprovedRepository(destination))
        {
            throw new GitHubAuthorizationUnavailable();
        }
    }

    /// <summary>
    /// Validates the destination before any authorization header is attached.
    /// </summary>
    /// <param name="request">The outgoing request.</param>
    /// <exception cref="GitHubAuthorizationUnavailable">The API request is outside the approved boundary.</exception>
    public void RequireRequest(HttpRequestMessage request)
    {
        Validate();
        var uri = request.RequestUri;
        if (!TrustedUri(uri))
        {
            throw new GitHubAuthorizationUnavailable();
        }
        if (!RepositoryRequest(request) && !SearchRequest(request))
        {
            throw new GitHubAuthorizationUnavailable();
        }
    }

    /// <summary>
    /// Gets an explicitly approved fallback only if no App configuration exists.
    /// </summary>
    /// <returns>The configured fallback token.</returns>
    /// <exception cref="GitHubAuthorizationUnavailable">Fallback is not explicitly and exclusively configured.</exception>
    public string Fallback()
    {
        Validate();
        if (!CanFallback())
        {
            throw new GitHubAuthorizationUnavailable();
        }
        return _fallback;
    }

    /// <summary>
    /// Checks that the mint response contains exactly the approved repositories.
    /// </summary>
    /// <param name="repositories">The repositories reported by GitHub.</param>
    /// <returns>Whether the response matches the explicit allowlist.</returns>
    public bool Matches(IEnumerable<string> repositories) => _repositories.SetEquals(repositories);

    static bool TrustedUri(Uri? uri) => uri is { IsAbsoluteUri: true, IsDefaultPort: true, UserInfo.Length: 0, Fragment.Length: 0 } &&
        uri.Scheme.Equals("https", StringComparison.Ordinal) && uri.Host.Equals("api.github.com", StringComparison.Ordinal);

    static bool IssueRoute(string[] parts, HttpMethod method) => parts.Length == 5 && (method == HttpMethod.Get || method == HttpMethod.Post);

    static bool CommentRoute(string[] parts, HttpMethod method) => parts is ["", "repos", _, _, "issues", var number, "comments"] && int.TryParse(number, out var parsed) && parsed > 0 && method == HttpMethod.Post;

    static bool Identifier(string value) => value.Length is > 0 and <= 100 && !value.Equals(".", StringComparison.Ordinal) && !value.Equals("..", StringComparison.Ordinal) && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');

    bool ValidConfiguration() => _api == "https://api.github.com/" && Identifier(_owner) && _repositories.Count > 0 && _repositories.All(ValidRepository);

    bool ValidRepository(string repository) => repository.Split('/') is [var owner, var name] && Identifier(name) && owner.Equals(_owner, StringComparison.OrdinalIgnoreCase);

    bool ApprovedRepository(GitHubDestination destination) => Identifier(destination.Owner) && Identifier(destination.Repository) && _repositories.Contains($"{destination.Owner}/{destination.Repository}");

    bool CanFallback() => !HasApp && _fallbackAllowed && _fallback.Length is > 0 and <= 1024 && _fallback.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-');

    bool RepositoryRequest(HttpRequestMessage request)
    {
        var uri = request.RequestUri!;
        var parts = uri.AbsolutePath.Split('/');
        if (parts is not ["", "repos", var owner, var repository, "issues", ..] || uri.Query.Length > 0)
        {
            return false;
        }
        RequireRepository(new(owner, repository));
        return IssueRoute(parts, request.Method) || CommentRoute(parts, request.Method);
    }

    bool SearchRequest(HttpRequestMessage request)
    {
        var uri = request.RequestUri!;
        if (uri.AbsolutePath != "/search/issues" || request.Method != HttpMethod.Get)
        {
            return false;
        }
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query)["q"] ?? string.Empty;
        var scopes = query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(part => part.StartsWith("repo:", StringComparison.OrdinalIgnoreCase)).ToArray();
        return scopes is [var scope] && scope[5..].Split('/') is [var owner, var repository] && ApprovedRepository(new(owner, repository));
    }
}
