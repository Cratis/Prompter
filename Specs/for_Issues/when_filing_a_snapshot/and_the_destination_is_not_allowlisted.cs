// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;
using Cratis.Prompter.GitHub.Authentication;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_Issues.when_filing_a_snapshot;

public class and_the_destination_is_not_allowlisted : Specification
{
    int _refused;

    async Task Because()
    {
        using var wire = new NoWire();
        using var client = new HttpClient(wire, disposeHandler: false) { BaseAddress = new("https://api.github.com/") };
        var options = Options.Create(new PrompterOptions { GitHub = new() { InstallationRepositories = ["Cratis/Chronicle"] } });
        var issues = new Issues(client, options, NullLogger<Issues>.Instance);
        foreach (var payload in new PendingIssue[] { new("Other", "Chronicle", "Title", "Body", []), new("Cratis", "Other", "Title", "Body", []) })
        {
            try { await issues.File(payload); }
            catch (GitHubAuthorizationUnavailable) { _refused++; }
        }
        try { await issues.File("Other", "Title", "Body", []); }
        catch (GitHubAuthorizationUnavailable) { _refused++; }
    }

    [Fact] void should_enforce_owner_and_repository_for_both_filing_apis() => _refused.ShouldEqual(3);

    sealed class NoWire : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException("A forbidden request reached the wire"));
    }
}
