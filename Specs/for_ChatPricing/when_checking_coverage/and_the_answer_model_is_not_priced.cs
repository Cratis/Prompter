// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_ChatPricing.when_checking_coverage;

public class and_the_answer_model_is_not_priced : Specification
{
    PrompterOptions _options = null!;

    void Establish()
    {
        _options = new();
        _options.Protection.Anthropic = new() { Model = _options.Anthropic.Model, InputUsdPerMillion = 4, OutputUsdPerMillion = 10, MaxInputTokens = 32000 };
        _options.Anthropic.Answer.Model = "another-model";
    }

    [Fact] void should_not_fall_back_to_another_models_prices() => ChatPricing.Covered(_options).ShouldBeFalse();
}
