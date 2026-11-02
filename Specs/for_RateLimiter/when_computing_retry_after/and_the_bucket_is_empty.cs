// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_RateLimiter.when_computing_retry_after;

public class and_the_bucket_is_empty : Specification
{
    readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    RateLimiter _limiter = null!;
    TimeSpan _empty;
    TimeSpan _later;
    TimeSpan _unknown;
    bool _allowedAfterWaiting;

    void Establish()
    {
        _limiter = new RateLimiter(Options.Create(new PrompterOptions()));
        for (var i = 0; i < 10; i++)
        {
            _limiter.TryConsume("alice", _now);
        }
    }

    void Because()
    {
        _empty = _limiter.RetryAfter("alice", _now);
        _later = _limiter.RetryAfter("alice", _now.AddSeconds(30));
        _unknown = _limiter.RetryAfter("bob", _now);
        _allowedAfterWaiting = _limiter.TryConsume("alice", _now + _empty);
    }

    [Fact] void should_wait_for_one_token_to_refill() => _empty.ShouldEqual(TimeSpan.FromSeconds(60));
    [Fact] void should_shorten_the_wait_as_time_passes() => _later.ShouldEqual(TimeSpan.FromSeconds(30));
    [Fact] void should_not_wait_for_an_unknown_user() => _unknown.ShouldEqual(TimeSpan.Zero);
    [Fact] void should_admit_once_the_wait_has_passed() => _allowedAfterWaiting.ShouldBeTrue();
}
