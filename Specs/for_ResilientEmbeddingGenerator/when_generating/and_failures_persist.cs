// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Embeddings;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_ResilientEmbeddingGenerator.when_generating;

public class and_failures_persist : Specification
{
    TransientlyFailingEmbeddingGenerator _inner = null!;
    ResilientEmbeddingGenerator _generator = null!;
    Exception _exception = null!;

    void Establish()
    {
        _inner = new TransientlyFailingEmbeddingGenerator(failuresBeforeSuccess: int.MaxValue);
        var options = Options.Create(new PrompterOptions { Voyage = { MaxRetries = 3, RetryBaseDelayMilliseconds = 0 } });
        _generator = new ResilientEmbeddingGenerator(_inner, options, NullLogger<ResilientEmbeddingGenerator>.Instance);
    }

    void Because()
    {
        try
        {
            _generator.GenerateAsync(["one"]).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            _exception = exception;
        }
    }

    [Fact] void should_try_the_initial_call_plus_every_retry() => _inner.Calls.ShouldEqual(4);
    [Fact] void should_surface_the_last_failure() => (_exception is HttpRequestException).ShouldBeTrue();
}
