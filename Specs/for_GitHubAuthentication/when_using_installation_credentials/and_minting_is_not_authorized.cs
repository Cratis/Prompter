// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Security.Cryptography;
using Cratis.Prompter.GitHub.Authentication;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_GitHubAuthentication.when_using_installation_credentials;

public class and_minting_is_not_authorized : Specification
{
    [Fact]
    public async Task should_reject_overprivileged_or_invalid_responses()
    {
        foreach (var response in new[]
        {
            "{}",
            "{\"token\":\"synthetic\",\"expires_at\":\"2020-01-01T00:00:00Z\"}",
            "{\"token\":\"synthetic\",\"expires_at\":\"2023-11-14T23:13:20Z\",\"permissions\":{\"issues\":\"write\",\"contents\":\"write\"},\"repositories\":[{\"full_name\":\"Cratis/Chronicle\"}]}",
            "{\"token\":\"synthetic\",\"expires_at\":\"2023-11-14T23:13:20Z\",\"permissions\":{\"issues\":\"write\"},\"repositories\":[{\"full_name\":\"Cratis/Other\"}]}"
        })
        {
            using var rsa = RSA.Create(2048);
            var key = Substitute.For<IAppKey>();
            key.Read(Arg.Any<CancellationToken>()).Returns(rsa.ExportPkcs8PrivateKeyPem());
            var options = new GitHubOptions { AppId = 123, InstallationId = 456, AppPrivateKeyPath = "synthetic", InstallationRepositories = ["Cratis/Chronicle"] };
            using var mint = new Mint(response);
            using var client = new HttpClient(mint, disposeHandler: false);
            using var tokens = new AppTokens(client, key, Options.Create(new PrompterOptions { GitHub = options }), new ControlledTime(DateTimeOffset.FromUnixTimeSeconds(1700000000)));
            var rejected = false;
            try { await tokens.Get(CancellationToken.None); }
            catch (GitHubAuthorizationUnavailable) { rejected = true; }
            rejected.ShouldBeTrue();
        }
    }

    sealed class Mint(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent(body) });
    }
}
