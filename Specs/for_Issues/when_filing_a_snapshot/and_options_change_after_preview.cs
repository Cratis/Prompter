// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.GitHub;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_Issues.when_filing_a_snapshot;

public class and_options_change_after_preview : Specification
{
    readonly Capture _wire = new();
    PendingIssue _payload = null!;
    string _url = null!;

    async Task Because()
    {
        var options = new GitHubOptions { InstallationRepositories = ["Cratis/Chronicle"] };
        var draft = new IssueDraft("Title", "Body", IssueKind.Bug, "chronicle");
        _payload = IssueComposition.Payload(draft, "Chronicle", "https://discord.com/channels/2/3", options);
        using var client = new HttpClient(_wire) { BaseAddress = new("https://api.github.com/") };
        var issues = new Issues(client, Options.Create(new PrompterOptions { GitHub = options }), Microsoft.Extensions.Logging.Abstractions.NullLogger<Issues>.Instance);
        options.Owner = "Changed";
        options.IssueLabel = "Changed";
        options.Repositories["chronicle"] = "Changed";
        options.InstallationRepositories.Clear();
        var filed = await issues.File(_payload);
        _url = filed.Url;
    }

    [Fact] void should_post_to_the_previewed_owner_and_repository() => _wire.Path.ShouldEqual("/repos/Cratis/Chronicle/issues");
    [Fact] void should_post_the_exact_body_and_labels() => _wire.Body.ShouldEqual(System.Text.Json.JsonSerializer.Serialize(new { title = _payload.Title, body = _payload.Body, labels = _payload.Labels }));
    [Fact] void should_return_the_created_issue() => _url.ShouldEqual("https://github.com/Cratis/Chronicle/issues/1");

    sealed class Capture : HttpMessageHandler
    {
        public string Path { get; private set; } = string.Empty;
        public string Body { get; private set; } = string.Empty;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri!.AbsolutePath;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new(HttpStatusCode.Created) { Content = new StringContent("{\"number\":1,\"html_url\":\"https://github.com/Cratis/Chronicle/issues/1\"}") };
        }
    }
}
