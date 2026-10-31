// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_clarifying;

public class and_clarification_is_disabled : given.a_grounded_answerer
{
    Answers _custom = null!;

    void Establish()
    {
        Found(new Passage(new("https://cratis.io/chronicle/a/"), "T", "H", "c", 0.02, 0.80), new Passage(new("https://cratis.io/arc/b/"), "T", "H", "c", 0.01, 0.79));
        _custom = new(passages, chat, interactions, Options.Create(new PrompterOptions { Answering = { ClarifyAmbiguousProducts = false } }), NullLogger<Answers>.Instance);
    }

    async Task Because() => result = await _custom.For(new("How do I handle commands?") { AllowClarification = true }, "discord-ask", TestContext.Current.CancellationToken);

    [Fact] void should_not_clarify() => result.Clarification.ShouldBeNull();
}
