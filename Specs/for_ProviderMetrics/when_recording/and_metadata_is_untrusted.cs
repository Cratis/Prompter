// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Telemetry;

namespace Cratis.Prompter.Specs.for_ProviderMetrics.when_recording;

public class and_metadata_is_untrusted : Specification
{
    [Fact]
    void should_recognize_the_fixed_issue_draft_task()
    {
        var tags = ProviderMetrics.Labels(new(Guid.NewGuid(), "anthropic", "primary", "issue-draft", 1, 10, 10, "claude-sonnet-5-5", string.Empty), "success");
        tags.Single(tag => tag.Key == "task").Value.ShouldEqual("issue-draft");
    }

    [Fact]
    void should_allow_only_fixed_label_values()
    {
        var tags = ProviderMetrics.Labels(new(Guid.NewGuid(), "private", "private", "private", 1, 10, 10, "private", "private"), "private");

        tags.Select(tag => tag.Key).ShouldEqual(["provider", "model", "task", "account", "operation", "outcome"]);
        tags.Select(tag => tag.Value).ShouldEachConformTo(value => Equals(value, "other"));
    }
}
