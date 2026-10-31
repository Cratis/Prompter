// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_AccountSelection.when_selecting;

public class an_explicit_secondary : Specification
{
    string _account = null!;

    void Because() => _account = AccountSelection.Select(new() { PreferredAccount = "secondary", AllowSecondary = true }, null, true, DateTimeOffset.UnixEpoch);

    [Fact] void should_not_require_a_fabricated_reset() => _account.ShouldEqual("secondary");
}
