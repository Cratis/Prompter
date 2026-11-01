// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_FeedbackVotes.when_voting;

public class and_the_budget_is_exceeded : given.a_feedback_budget
{
    bool[] _spent = null!;
    bool _afterTheWindow;

    void Because()
    {
        Create();
        _spent = [Votes.TrySpend(7), Votes.TrySpend(7), Votes.TrySpend(7), Votes.TrySpend(7)];
        Clock.Advance(TimeSpan.FromSeconds(600));
        _afterTheWindow = Votes.TrySpend(7);
    }

    [Fact] void should_allow_the_budget() => _spent.Take(3).All(allowed => allowed).ShouldBeTrue();
    [Fact] void should_refuse_the_next_click() => _spent[3].ShouldBeFalse();
    [Fact] void should_allow_clicks_again_after_the_window() => _afterTheWindow.ShouldBeTrue();
}
