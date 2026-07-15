// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Embeddings;

namespace Cratis.Prompter.Specs.for_EmbeddingRetry.when_computing_backoff;

public class and_retrying_repeatedly : Specification
{
    static readonly TimeSpan _baseDelay = TimeSpan.FromMilliseconds(500);

    [Fact] void should_wait_the_base_delay_on_the_first_attempt() => EmbeddingRetry.BackoffFor(0, _baseDelay).ShouldEqual(TimeSpan.FromMilliseconds(500));
    [Fact] void should_double_on_the_second_attempt() => EmbeddingRetry.BackoffFor(1, _baseDelay).ShouldEqual(TimeSpan.FromMilliseconds(1000));
    [Fact] void should_quadruple_on_the_third_attempt() => EmbeddingRetry.BackoffFor(2, _baseDelay).ShouldEqual(TimeSpan.FromMilliseconds(2000));
    [Fact] void should_keep_growing_exponentially() => EmbeddingRetry.BackoffFor(4, _baseDelay).ShouldEqual(TimeSpan.FromMilliseconds(8000));
}
