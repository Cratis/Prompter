// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Embeddings;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_ResilientEmbeddingGenerator.when_generating;

public class and_a_transient_failure_precedes_success : Specification
{
    TransientlyFailingEmbeddingGenerator _inner = null!;
    ResilientEmbeddingGenerator _generator = null!;
    GeneratedEmbeddings<Embedding<float>> _result = null!;

    void Establish()
    {
        _inner = new TransientlyFailingEmbeddingGenerator(failuresBeforeSuccess: 2);
        var options = Options.Create(new PrompterOptions { Voyage = { MaxRetries = 5, RetryBaseDelayMilliseconds = 0 } });
        _generator = new ResilientEmbeddingGenerator(_inner, options, NullLogger<ResilientEmbeddingGenerator>.Instance);
    }

    void Because() => _result = _generator.GenerateAsync(["one", "two"]).GetAwaiter().GetResult();

    [Fact] void should_eventually_succeed() => _result.Count.ShouldEqual(2);
    [Fact] void should_retry_until_it_succeeds() => _inner.Calls.ShouldEqual(3);
}
