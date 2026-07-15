// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Operations;

namespace Cratis.Prompter.Specs.for_ReindexAuth.when_authorizing;

public class and_the_secret_matches : Specification
{
    bool _result;

    void Because() => _result = ReindexAuth.IsAuthorized("s3cret", "s3cret");

    [Fact] void should_authorize() => _result.ShouldBeTrue();
}
