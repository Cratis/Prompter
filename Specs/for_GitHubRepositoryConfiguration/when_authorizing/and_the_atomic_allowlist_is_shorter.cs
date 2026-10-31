// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Cratis.Prompter.GitHub.Authentication;
using Cratis.Prompter.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_GitHubRepositoryConfiguration.when_authorizing;

public class and_the_atomic_allowlist_is_shorter
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_mint_only_the_exact_list_and_refuse_overbroad_responses(bool overbroad)
    {
        using var rsa = RSA.Create(2048);
        var key = Substitute.For<IAppKey>();
        key.Read(Arg.Any<CancellationToken>()).Returns(rsa.ExportRSAPrivateKeyPem());
        var options = Options.Create(Resolved());
        using var transport = new mint_transport(overbroad);
        using var client = new HttpClient(transport);
        using var tokens = new AppTokens(client, key, options, TimeProvider.System);
        var error = await Catch.Exception(() => tokens.Get(TestContext.Current.CancellationToken));
        transport.Repositories.Length.ShouldEqual(1);
        transport.Repositories[0].ShouldEqual("Arc");
        if (overbroad) error.ShouldBeOfExactType<GitHubAuthorizationUnavailable>();
        else error.ShouldBeNull();
    }

    [Fact]
    async Task should_deny_an_inherited_repository_before_any_mint()
    {
        var options = Options.Create(Resolved());
        var key = Substitute.For<IAppKey>();
        using var transport = new mint_transport(false);
        using var client = new HttpClient(transport);
        using var tokens = new AppTokens(client, key, options, TimeProvider.System);
        using var authentication = new GitHubAuthentication(new GitHubPolicy(options), tokens) { InnerHandler = transport };
        using var api = new HttpClient(authentication);
        var error = await Catch.Exception(() => api.GetAsync("https://api.github.com/repos/Cratis/Chronicle/issues", TestContext.Current.CancellationToken));
        error.ShouldBeOfExactType<GitHubAuthorizationUnavailable>();
        transport.Calls.ShouldEqual(0);
        await key.DidNotReceive().Read(Arg.Any<CancellationToken>());
    }

    PrompterOptions Resolved()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Prompter:GitHub:InstallationRepositories:0"] = "Cratis/Arc",
            ["Cratis:Prompter:GitHub:InstallationRepositories:1"] = "Cratis/Chronicle",
            ["Cratis:Prompter:GitHub:InstallationRepositoriesJson"] = "[\"Cratis/Arc\"]"
        }).Build();
        var options = new PrompterOptions { GitHub = new() { AppId = 123, InstallationId = 456, AppPrivateKeyPath = "/offline/key.pem" } };
        configuration.GetSection("Cratis:Prompter:GitHub").Bind(options.GitHub);
        GitHubRepositoryConfiguration.Apply(configuration, options);
        return options;
    }

    sealed class mint_transport(bool overbroad) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        public string[] Repositories { get; private set; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            Repositories = document.RootElement.GetProperty("repository_names").EnumerateArray().Select(value => value.GetString()!).ToArray();
            var repositories = overbroad ? new[] { "Cratis/Arc", "Cratis/Chronicle" } : ["Cratis/Arc"];
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    token = "synthetic_installation",
                    expires_at = DateTimeOffset.UtcNow.AddMinutes(30),
                    permissions = new { issues = "write" },
                    repositories = repositories.Select(name => new { full_name = name })
                }))
            };
        }
    }
}
