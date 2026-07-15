// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Embeddings;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_ResilientEmbeddingGenerator.when_generating;

public class and_the_failure_is_permanent : Specification
{
    TransientlyFailingEmbeddingGenerator _inner = null!;
    ResilientEmbeddingGenerator _generator = null!;
    Exception _exception = null!;

    void Establish()
    {
        _inner = new TransientlyFailingEmbeddingGenerator(failuresBeforeSuccess: 1, status: HttpStatusCode.BadRequest);
        var options = Options.Create(new PrompterOptions { Voyage = { MaxRetries = 5, RetryBaseDelayMilliseconds = 0 } });
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

    [Fact] void should_not_retry() => _inner.Calls.ShouldEqual(1);
    [Fact] void should_surface_the_failure() => (_exception is HttpRequestException).ShouldBeTrue();
}
