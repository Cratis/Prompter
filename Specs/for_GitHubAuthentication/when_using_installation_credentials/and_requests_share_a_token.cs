// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Security.Cryptography;
using System.Text;
using Cratis.Prompter.GitHub.Authentication;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_GitHubAuthentication.when_using_installation_credentials;

public class and_requests_share_a_token : Specification
{
    readonly ControlledTime _time = new(DateTimeOffset.FromUnixTimeSeconds(1700000000));
    readonly RecordingMint _mint = new();
    readonly IAppKey _key = Substitute.For<IAppKey>();
    AppTokens _tokens = null!;
    HttpClient _client = null!;
    string[] _received = [];

    void Establish()
    {
        using var rsa = RSA.Create(2048);
        _key.Read(Arg.Any<CancellationToken>()).Returns(rsa.ExportPkcs8PrivateKeyPem());
        var options = new GitHubOptions { AppId = 123, InstallationId = 456, AppPrivateKeyPath = "synthetic-key", InstallationRepositories = ["Cratis/Chronicle"] };
        _client = new(_mint);
        _tokens = new(_client, _key, Options.Create(new PrompterOptions { GitHub = options }), _time);
    }

    async Task Because()
    {
        _received = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => _tokens.Get(CancellationToken.None)));
        _time.Advance(TimeSpan.FromMinutes(59));
        await _tokens.Get(CancellationToken.None);
    }

    [Fact] void should_share_concurrent_refresh() => _mint.Calls.ShouldEqual(2);
    [Fact] void should_return_the_installation_token() => _received.All(value => value == "synthetic-installation-token").ShouldBeTrue();
    [Fact] void should_request_only_issue_write_and_named_repositories() => _mint.Body.ShouldEqual("{\"repository_names\":[\"Chronicle\"],\"permissions\":{\"issues\":\"write\"}}");
    [Fact] void should_send_a_short_lived_rs256_app_jwt() => _mint.ValidJwt.ShouldBeTrue();

    void Destroy()
    {
        _tokens.Dispose();
        _client.Dispose();
    }

    sealed class RecordingMint : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string Body { get; private set; } = string.Empty;
        public bool ValidJwt { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            request.Method.ShouldEqual(HttpMethod.Post);
            request.RequestUri!.AbsoluteUri.ShouldEqual("https://api.github.com/app/installations/456/access_tokens");
            var parts = request.Headers.Authorization!.Parameter!.Split('.');
            var header = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0].Replace('-', '+').Replace('_', '/') + new string('=', (4 - (parts[0].Length % 4)) % 4)));
            ValidJwt = header.Contains("RS256", StringComparison.Ordinal) && parts.Length == 3;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new(HttpStatusCode.Created)
            {
                Content = new StringContent("{\"token\":\"synthetic-installation-token\",\"expires_at\":\"" + (Calls == 1 ? "2023-11-14T23:13:20Z" : "2023-11-15T00:12:20Z") + "\",\"permissions\":{\"issues\":\"write\",\"metadata\":\"read\"},\"repositories\":[{\"full_name\":\"Cratis/Chronicle\"}]}")
            };
        }
    }
}
