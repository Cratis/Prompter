// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_SampleFiles.when_reading;

public class and_the_tree_is_curated : Specification
{
    readonly List<DocsPage> _pages = [];
    readonly ILogger<SampleFiles> _log = Substitute.For<ILogger<SampleFiles>>();
    Responses _responses = null!;
    HttpClient _client = null!;

    IEnumerable<string> Messages => _log.ReceivedCalls()
        .Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log))
        .Select(call => call.GetArguments()[2]?.ToString() ?? string.Empty);

    void Establish()
    {
        _client = new HttpClient(_responses = new Responses());
        _log.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }
    async Task Because()
    {
        var settings = new PrompterOptions();
        settings.Ingestion.Samples.MaxFiles = 3;
        var source = new SampleFiles(_client, Options.Create(settings), _log);
        await foreach (var page in source.GetPages()) _pages.Add(page);
    }

    void Destroy()
    {
        _client.Dispose();
        _responses.Dispose();
    }

    [Fact] void should_exclude_unlisted_generated_oversized_and_sensitive_files() => _pages.Count.ShouldEqual(2);
    [Fact] void should_put_code_first() => _pages[0].Url.Value.ShouldEqual("https://github.com/Cratis/Samples/blob/main/LearningPath/A.cs");
    [Fact] void should_stop_at_the_file_cap() => _pages[1].Url.Value.ShouldEqual("https://github.com/Cratis/Samples/blob/main/LearningPath/Z.cs");
    [Fact] void should_wrap_code() => _pages[0].Markdown.ShouldContain("```csharp\npublic record Sample();\n```");
    [Fact] void should_bound_raw_requests() => _responses.RawPaths.Count.ShouldEqual(3);
    [Fact] void should_identify_the_tree_request() => _responses.TreeRequest.ShouldEqual("https://api.github.com/repos/Cratis/Samples/git/trees/main?recursive=1");
    [Fact] void should_send_no_authorization() => _responses.Authorized.ShouldBeFalse();
    [Fact] void should_identify_every_request() => _responses.Identified.ShouldBeTrue();

    [Fact] void should_log_only_the_excluded_path() => Messages.Single().ShouldContain("LearningPath/B.cs");
    [Fact] void should_not_log_sensitive_contents() => Messages.Any(message => message.Contains(new string('a', 36), StringComparison.Ordinal)).ShouldBeFalse();

    sealed class Responses : HttpMessageHandler
    {
        public List<string> RawPaths = [];
        public string? TreeRequest;
        public bool Authorized;
        public bool Identified = true;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorized |= request.Headers.Authorization is not null;
            Identified &= request.Headers.UserAgent.ToString() == "Cratis-Prompter" && request.Headers.Accept.ToString() == "application/vnd.github+json";
            string body;
            if (request.RequestUri!.Host == "api.github.com")
            {
                TreeRequest = request.RequestUri.ToString();
                body = """
                    {"truncated":false,"tree":[
                      {"path":"README.md","type":"blob","size":10},
                      {"path":"LearningPath/A.cs","type":"blob","size":30},
                      {"path":"LearningPath/B.cs","type":"blob","size":50},
                      {"path":"LearningPath/Z.cs","type":"blob","size":30},
                      {"path":"LearningPath/Huge.cs","type":"blob","size":20000},
                      {"path":"LearningPath/Generated.cs","type":"blob","size":30},
                      {"path":"LearningPath/.cratis/A.cs","type":"blob","size":30},
                      {"path":"Other/A.cs","type":"blob","size":30},
                      {"path":"LearningPath/Folder.cs","type":"tree","size":0}]}
                    """;
            }
            else
            {
                RawPaths.Add(request.RequestUri.AbsolutePath);
                body = request.RequestUri.AbsolutePath.EndsWith("README.md", StringComparison.Ordinal) ? "Read this." : "public record Sample();";
                if (request.RequestUri.AbsolutePath.EndsWith("B.cs", StringComparison.Ordinal))
                {
                    const string prefix = "ghp";
                    body = prefix + "_" + new string('a', 36);
                }
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        }
    }
}
