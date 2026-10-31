// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_ChatPricing.when_selecting;

public class the_task_prices : Specification
{
    PrompterOptions _options = null!;

    void Establish()
    {
        _options = new();
        _options.Protection.Anthropic = new() { Model = "claude-sonnet-5-5", InputUsdPerMillion = 4, OutputUsdPerMillion = 10, MaxInputTokens = 32000 };
    }

    [Fact] void should_price_answers_with_the_answer_table() => ChatPricing.ForTask(_options, "answer").ShouldEqual(_options.Protection.AnthropicAnswer);
    [Fact] void should_price_issue_drafts_with_the_draft_table() => ChatPricing.ForTask(_options, "issue-draft").ShouldEqual(_options.Protection.Anthropic);
    [Fact] void should_resolve_the_default_answer_model_to_haiku() => ChatPricing.Model(_options, "answer").ShouldEqual("claude-haiku-5-5");
    [Fact] void should_resolve_the_draft_model_to_the_fallback() => ChatPricing.Model(_options, "issue-draft").ShouldEqual(_options.Anthropic.Model);
    [Fact] void should_cover_both_tasks_by_default() => ChatPricing.Covered(_options).ShouldBeTrue();
    [Fact] void should_reserve_the_larger_task_exposure() => ChatPricing.MaximumExposure(_options).ShouldEqual(_options.Protection.Anthropic.Cost(32000, _options.Anthropic.IssueDraft.MaxOutputTokens));
}
