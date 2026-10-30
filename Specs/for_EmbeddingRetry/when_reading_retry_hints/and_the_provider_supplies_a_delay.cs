// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Embeddings;

namespace Cratis.Prompter.Specs.for_EmbeddingRetry.when_reading_retry_hints;

public class and_the_provider_supplies_a_delay : Specification
{
    static readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact] void should_read_delta_seconds() => EmbeddingRetry.RetryAfter("12", _now).ShouldEqual(TimeSpan.FromSeconds(12));
    [Fact] void should_read_an_http_date() => EmbeddingRetry.RetryAfter("Thu, 01 Jan 2026 00:00:20 GMT", _now).ShouldEqual(TimeSpan.FromSeconds(20));
    [Fact] void should_ignore_malformed_hints() => EmbeddingRetry.RetryAfter("later", _now).ShouldBeNull();
    [Fact] void should_ignore_missing_hints() => EmbeddingRetry.RetryAfter(null, _now).ShouldBeNull();
    [Fact] void should_not_wait_for_a_past_date() => EmbeddingRetry.RetryAfter("Wed, 31 Dec 2025 00:00:00 GMT", _now).ShouldEqual(TimeSpan.Zero);
}
