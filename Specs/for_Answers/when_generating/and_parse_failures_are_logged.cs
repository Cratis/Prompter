// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_generating;

public class and_parse_failures_are_logged : given.a_grounded_answerer
{
    const string Response = "{\"text\":\"An incomplete answer";
    readonly ILogger<Answers> _logger = Substitute.For<ILogger<Answers>>();
    Answers _answerer = null!;

    IEnumerable<KeyValuePair<string, object?>>[] Warnings => Logs(LogLevel.Warning);

    KeyValuePair<string, object?>[] Information => Logs(LogLevel.Information).Single().ToArray();

    void Establish()
    {
        Found(Passage(0.8));
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _answerer = new(passages, chat, interactions, Options.Create(options), _logger);
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, Response)) { FinishReason = ChatFinishReason.Length }));
    }

    async Task Because() => result = await _answerer.For(new("Question?"), "cli", TestContext.Current.CancellationToken);

    [Fact] void should_log_both_parse_failures() => Warnings.Length.ShouldEqual(2);
    [Fact] void should_log_only_length_and_finish_reason() => Warnings.All(values => values.Select(pair => pair.Key).SequenceEqual(["ResponseLength", "FinishReason", "{OriginalFormat}"])).ShouldBeTrue();
    [Fact] void should_report_the_response_length() => Warnings.All(values => Equals(values.Single(pair => pair.Key == "ResponseLength").Value, Response.Length)).ShouldBeTrue();
    [Fact] void should_report_the_finish_reason() => Warnings.All(values => Equals(values.Single(pair => pair.Key == "FinishReason").Value, ChatFinishReason.Length.ToString())).ShouldBeTrue();
    [Fact] void should_log_only_the_top_qualified_page_and_similarity_at_information() => Information.Select(pair => pair.Key).ShouldEqual(["PageUrl", "Similarity", "{OriginalFormat}"]);
    [Fact] void should_log_the_page_url() => Information.Single(pair => pair.Key == "PageUrl").Value.ShouldEqual("https://cratis.io/chronicle/");
    [Fact] void should_log_the_qualified_similarity() => Information.Single(pair => pair.Key == "Similarity").Value.ShouldEqual(0.8);

    IEnumerable<KeyValuePair<string, object?>>[] Logs(LogLevel level) =>
        [.. _logger.ReceivedCalls().Where(call => call.GetMethodInfo().Name == "Log" && Equals(call.GetArguments()[0], level))
            .Select(call => (IEnumerable<KeyValuePair<string, object?>>)call.GetArguments()[2]!)];
}
