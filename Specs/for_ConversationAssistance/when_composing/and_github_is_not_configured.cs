// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Hosting;
using Cratis.Prompter.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Prompter.Specs.for_ConversationAssistance.when_composing;

public class and_github_is_not_configured : Specification
{
    IHost _host = null!;
    Classification _classification = null!;
    IReadOnlyList<RelatedIssue> _hints = [];
    IReadOnlyList<ExistingIssue> _duplicates = [];
    bool _assistanceRegistered;

    void Establish()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Prompter:Protection:AnsweringEnabled"] = "false",
            ["Cratis:Prompter:Protection:IndexingEnabled"] = "false"
        });
        builder.AddPrompter();
        _assistanceRegistered = builder.Services.Any(service => service.ServiceType == typeof(ConversationAssistance));
        _host = builder.Build();
    }

    async Task Because()
    {
        await _host.StartAsync();
        _classification = await _host.Services.GetRequiredService<IRequestClassifier>().Classify("Arc question", []);
        _hints = await _host.Services.GetRequiredService<IRelatedIssues>().FindRelated("arc", "Arc question");
        _duplicates = await _host.Services.GetRequiredService<IIssues>().FindSimilar("Arc", "Arc question");
        await _host.StopAsync();
    }

    void Destroy() => _host.Dispose();

    [Fact] void should_register_conversation_assistance() => _assistanceRegistered.ShouldBeTrue();
    [Fact] void should_use_the_deterministic_classifier_without_a_laya_endpoint() => _classification.Source.ShouldEqual(ClassificationSource.Deterministic);
    [Fact] void should_take_product_keys_from_the_host_mapping() => _classification.Product.Value.ShouldEqual("arc");
    [Fact] void should_disable_related_search_without_failing_startup() => _hints.ShouldBeEmpty();
    [Fact] void should_disable_explicit_preview_lookup_without_credentials() => _duplicates.ShouldBeEmpty();
}
