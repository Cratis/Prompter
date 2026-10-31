// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.GitHub.Authentication;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_GitHubAuthentication.when_authorizing_requests;

public class and_the_target_is_untrusted : Specification
{
    [Fact]
    public async Task should_never_attach_credentials_to_an_unapproved_target()
    {
        var options = Options.Create(new PrompterOptions { GitHub = new() { Token = "synthetic-fallback", AllowTokenFallback = true, InstallationRepositories = ["Cratis/Chronicle"] } });
        using var wire = new Wire();
        using var authentication = new GitHubAuthentication(new GitHubPolicy(options), null) { InnerHandler = wire };
        using var client = new HttpClient(authentication, disposeHandler: false);
        foreach (var uri in new[] { "http://api.github.com/repos/Cratis/Chronicle/issues", "https://evil.invalid/repos/Cratis/Chronicle/issues", "https://api.github.com/repos/Cratis/Other/issues", "https://api.github.com/user", "https://api.github.com/app/installations/456/access_tokens" })
        {
            var rejected = false;
            try { using var response = await client.GetAsync(uri, TestContext.Current.CancellationToken); }
            catch (GitHubAuthorizationUnavailable) { rejected = true; }
            rejected.ShouldBeTrue();
        }
        wire.Calls.ShouldEqual(0);
    }

    [Fact]
    public async Task should_require_explicit_fallback_permission()
    {
        var policy = new GitHubPolicy(Options.Create(new PrompterOptions { GitHub = new() { Token = "synthetic", InstallationRepositories = ["Cratis/Chronicle"] } }));
        using var wire = new Wire();
        using var authentication = new GitHubAuthentication(policy, null) { InnerHandler = wire };
        using var client = new HttpClient(authentication, disposeHandler: false);
        var rejected = false;
        try { using var response = await client.GetAsync("https://api.github.com/repos/Cratis/Chronicle/issues", TestContext.Current.CancellationToken); }
        catch (GitHubAuthorizationUnavailable) { rejected = true; }
        rejected.ShouldBeTrue();
    }

    [Fact]
    public async Task should_not_replay_an_ambiguous_write()
    {
        var policy = new GitHubPolicy(Options.Create(new PrompterOptions { GitHub = new() { Token = "synthetic", AllowTokenFallback = true, InstallationRepositories = ["Cratis/Chronicle"] } }));
        using var wire = new Wire();
        using var authentication = new GitHubAuthentication(policy, null) { InnerHandler = wire };
        using var client = new HttpClient(authentication, disposeHandler: false);
        using var body = new StringContent("{}");
        using var response = await client.PostAsync("https://api.github.com/repos/Cratis/Chronicle/issues", body, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
        wire.Calls.ShouldEqual(1);
    }

    sealed class Wire : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        }
    }
}
