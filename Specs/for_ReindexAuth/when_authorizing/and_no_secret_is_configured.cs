// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Operations;

namespace Cratis.Prompter.Specs.for_ReindexAuth.when_authorizing;

public class and_no_secret_is_configured : Specification
{
    bool _result;

    void Because() => _result = ReindexAuth.IsAuthorized("anything", string.Empty);

    [Fact] void should_refuse_every_caller() => _result.ShouldBeFalse();
}
