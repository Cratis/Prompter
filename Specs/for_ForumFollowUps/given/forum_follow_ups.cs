// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Specs.Fakes;

namespace Cratis.Prompter.Specs.for_ForumFollowUps.given;

public class forum_follow_ups : Specification
{
    protected ControlledTime Clock { get; private set; } = null!;
    protected PrompterOptions Options { get; private set; } = null!;
    protected ForumFollowUps FollowUps { get; private set; } = null!;

    void Establish()
    {
        Clock = new(new(2026, 6, 5, 12, 0, 0, TimeSpan.Zero));
        Options = new();
        FollowUps = new(Microsoft.Extensions.Options.Options.Create(Options), Clock);
    }
}
