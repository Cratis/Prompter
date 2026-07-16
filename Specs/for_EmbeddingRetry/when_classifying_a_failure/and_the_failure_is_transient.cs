// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Embeddings;

namespace Cratis.Prompter.Specs.for_EmbeddingRetry.when_classifying_a_failure;

public class and_the_failure_is_transient : Specification
{
    [Fact] void should_retry_on_rate_limiting() => EmbeddingRetry.IsTransient(HttpStatusCode.TooManyRequests).ShouldBeTrue();
    [Fact] void should_retry_on_internal_server_error() => EmbeddingRetry.IsTransient(HttpStatusCode.InternalServerError).ShouldBeTrue();
    [Fact] void should_retry_on_bad_gateway() => EmbeddingRetry.IsTransient(HttpStatusCode.BadGateway).ShouldBeTrue();
    [Fact] void should_retry_on_service_unavailable() => EmbeddingRetry.IsTransient(HttpStatusCode.ServiceUnavailable).ShouldBeTrue();
    [Fact] void should_retry_when_the_request_never_got_a_status() => EmbeddingRetry.IsTransient(null).ShouldBeTrue();
}
