// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Embeddings;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ResilientEmbeddingGenerator.when_generating;

public class and_retry_after_exceeds_the_deadline : Specification
{
    [Fact]
    public async Task should_fail_without_retrying_and_share_the_cooldown()
    {
        var inner = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();
        var accounting = Substitute.For<IProviderAccounting>();
        accounting.Reserve(Arg.Any<ProviderDispatch>(), Arg.Any<CancellationToken>()).Returns(true);
        inner.GenerateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<EmbeddingGenerationOptions>(), Arg.Any<CancellationToken>())
            .Returns<GeneratedEmbeddings<Embedding<float>>>(_ => throw new VoyageRequestFailed(HttpStatusCode.TooManyRequests, TimeSpan.FromSeconds(120)));
        using var generator = new ResilientEmbeddingGenerator(inner, Options.Create(new PrompterOptions()), NullLogger<ResilientEmbeddingGenerator>.Instance, accounting);

        await Assert.ThrowsAsync<EmbeddingGenerationFailed>(() => generator.GenerateAsync(["dummy"], cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<EmbeddingGenerationFailed>(() => generator.GenerateAsync(["dummy"], cancellationToken: TestContext.Current.CancellationToken));

        await inner.Received(1).GenerateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<EmbeddingGenerationOptions>(), Arg.Any<CancellationToken>());
        await accounting.Received(1).Complete(Arg.Is<ProviderDispatch>(dispatch => dispatch.Provider == "voyage" && dispatch.Account == "primary"), null, "unknown");
    }
}
