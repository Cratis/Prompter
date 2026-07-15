// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Embeddings;

namespace Cratis.Prompter.Specs.for_EmbeddingRetry.when_classifying_a_failure;

public class and_the_failure_is_permanent : Specification
{
    [Fact] void should_not_retry_on_bad_request() => EmbeddingRetry.IsTransient(HttpStatusCode.BadRequest).ShouldBeFalse();
    [Fact] void should_not_retry_on_unauthorized() => EmbeddingRetry.IsTransient(HttpStatusCode.Unauthorized).ShouldBeFalse();
    [Fact] void should_not_retry_on_not_found() => EmbeddingRetry.IsTransient(HttpStatusCode.NotFound).ShouldBeFalse();
    [Fact] void should_not_retry_when_the_status_is_unknown() => EmbeddingRetry.IsTransient(null).ShouldBeFalse();
}
