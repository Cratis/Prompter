// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.GitHub.Authentication;

/// <summary>
/// Mints least-privilege installation tokens, with one refresh and an expiry safety margin.
/// </summary>
/// <param name="client">The isolated, non-redirecting token-mint transport.</param>
/// <param name="key">The approved private key source.</param>
/// <param name="options">The installation configuration to pin.</param>
/// <param name="timeProvider">The expiry and JWT clock.</param>
public sealed class AppTokens(HttpClient client, IAppKey key, IOptions<PrompterOptions> options, TimeProvider timeProvider) : IDisposable
{
    readonly GitHubPolicy _policy = new(options);
    readonly SemaphoreSlim _refresh = new(1, 1);
    string _token = string.Empty;
    DateTimeOffset _expires;

    /// <summary>
    /// Gets a cached token or mints one. No fallback or retry follows a failed mint.
    /// </summary>
    /// <param name="cancellationToken">Cancels waiting, key access and HTTP.</param>
    /// <returns>The installation token, never an App JWT.</returns>
    /// <exception cref="GitHubAuthorizationUnavailable">The installation could not be authorized.</exception>
    public async Task<string> Get(CancellationToken cancellationToken)
    {
        _policy.Validate();
        await _refresh.WaitAsync(cancellationToken);
        try
        {
            if (_token.Length > 0 && _expires > timeProvider.GetUtcNow().AddMinutes(1))
            {
                return _token;
            }
            _token = string.Empty;
            _expires = default;
            return await Mint(cancellationToken);
        }
        catch (Exception error) when (error is HttpRequestException or JsonException or CryptographicException or FormatException or InvalidOperationException or KeyNotFoundException or ArgumentException)
        {
            throw new GitHubAuthorizationUnavailable();
        }
        finally
        {
            _refresh.Release();
        }
    }

    /// <summary>
    /// Invalidates a rejected token without retrying the request that used it.
    /// </summary>
    /// <param name="token">The token rejected by GitHub.</param>
    /// <param name="cancellationToken">Cancels waiting for a refresh.</param>
    /// <returns>A task representing invalidation.</returns>
    public async Task Invalidate(string token, CancellationToken cancellationToken)
    {
        await _refresh.WaitAsync(cancellationToken);
        try
        {
            if (_token == token)
            {
                _token = string.Empty;
                _expires = default;
            }
        }
        finally
        {
            _refresh.Release();
        }
    }

    /// <inheritdoc/>
    public void Dispose() => _refresh.Dispose();

    static bool UsableToken(string token) => token.Length is > 0 and <= 1024 && token.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-');

    static bool LimitedPermissions(JsonElement permissions) => permissions.GetProperty("issues").GetString() == "write" && permissions.EnumerateObject().All(AllowedPermission);

    static bool AllowedPermission(JsonProperty permission) => permission.Name switch
    {
        "issues" => permission.Value.GetString() == "write",
        "metadata" => permission.Value.GetString() == "read",
        _ => false
    };

    static string Encode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    async Task<string> Mint(CancellationToken cancellationToken)
    {
        if (_policy.AppId <= 0 || _policy.InstallationId <= 0)
        {
            throw new GitHubAuthorizationUnavailable();
        }
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.github.com/app/installations/{_policy.InstallationId.ToString(CultureInfo.InvariantCulture)}/access_tokens");
        request.Headers.Authorization = new("Bearer", await Jwt(cancellationToken));
        request.Headers.UserAgent.Add(new("Cratis-Prompter", "1.0"));
        request.Headers.Accept.Add(new("application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        request.Content = JsonContent.Create(new { repository_names = _policy.RepositoryNames, permissions = new { issues = "write" } });
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode != System.Net.HttpStatusCode.Created)
        {
            throw new GitHubAuthorizationUnavailable();
        }
        await response.Content.LoadIntoBufferAsync(65536, cancellationToken);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        if (!ValidResponse(root, out var token, out var expires))
        {
            throw new GitHubAuthorizationUnavailable();
        }
        _token = token;
        _expires = expires;
        return _token;
    }

    bool ValidResponse(JsonElement root, out string token, out DateTimeOffset expires)
    {
        token = root.GetProperty("token").GetString() ?? string.Empty;
        expires = root.GetProperty("expires_at").GetDateTimeOffset();
        return UsableToken(token) && UsableExpiry(expires) && LimitedPermissions(root.GetProperty("permissions")) &&
            _policy.Matches(root.GetProperty("repositories").EnumerateArray().Select(repository => repository.GetProperty("full_name").GetString() ?? string.Empty));
    }

    bool UsableExpiry(DateTimeOffset expires) => expires > timeProvider.GetUtcNow().AddMinutes(1) && expires <= timeProvider.GetUtcNow().AddMinutes(70);

    async Task<string> Jwt(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var header = Encode(Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"typ\":\"JWT\"}"));
        var payload = Encode(JsonSerializer.SerializeToUtf8Bytes(new { iat = now.AddSeconds(-60).ToUnixTimeSeconds(), exp = now.AddMinutes(8).ToUnixTimeSeconds(), iss = _policy.AppId.ToString(CultureInfo.InvariantCulture) }));
        using var rsa = RSA.Create();
        rsa.ImportFromPem(await key.Read(cancellationToken));
        if (rsa.KeySize < 2048)
        {
            throw new GitHubAuthorizationUnavailable();
        }
        var content = $"{header}.{payload}";
        return $"{content}.{Encode(rsa.SignData(Encoding.ASCII.GetBytes(content), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))}";
    }
}
