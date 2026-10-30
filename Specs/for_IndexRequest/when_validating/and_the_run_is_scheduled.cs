// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_IndexRequest.when_validating;

public class and_the_run_is_scheduled : Specification
{
    Exception? _error;
    void Because() => _error = Catch.Exception(() => new IndexRequest(true, "FULL REINDEX", true).Validate());

    [Fact] void should_reject_full_even_with_confirmation() => _error.ShouldBeOfExactType<IndexConsentRequired>();
}
