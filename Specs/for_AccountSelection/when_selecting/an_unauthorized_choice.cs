// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_AccountSelection.when_selecting;

public class an_unauthorized_choice : Specification
{
    [Theory]
    [InlineData("secondary", false, true, true)]
    [InlineData("secondary", true, false, true)]
    [InlineData("secondary", true, true, false)]
    [InlineData("unknown", true, true, true)]
    [InlineData("Secondary", true, true, true)]
    public void should_fail_closed(string preference, bool allowed, bool client, bool accounting) =>
        Catch.Exception(() => AccountSelection.Select(new() { PreferredAccount = preference, AllowSecondary = allowed }, null, client && accounting, DateTimeOffset.UnixEpoch)).ShouldBeOfExactType<ProviderUnavailable>();
}
