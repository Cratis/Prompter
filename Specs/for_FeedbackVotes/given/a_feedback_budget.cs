// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_FeedbackVotes.given;

public class a_feedback_budget : Specification
{
    protected FeedbackVotes Votes { get; private set; } = null!;
    protected PrompterOptions Options { get; private set; } = null!;
    protected ControlledTime Clock { get; private set; } = null!;

    void Establish()
    {
        Options = new();
        Options.Protection.FeedbackClicksPerUser = 3;
        Clock = new(new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    protected void Create() => Votes = new(Microsoft.Extensions.Options.Options.Create(Options), Clock);
}
