// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_RateLimiter.when_consuming;

public class and_a_user_is_within_the_limit : Specification
{
    readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    RateLimiter _limiter = null!;
    int _allowed;

    void Establish() => _limiter = new RateLimiter(Options.Create(new PrompterOptions()));

    void Because() => _allowed = Enumerable.Range(0, 5).Count(_ => _limiter.TryConsume("user", _now));

    [Fact] void should_allow_the_full_default_allowance() => _allowed.ShouldEqual(5);
}
