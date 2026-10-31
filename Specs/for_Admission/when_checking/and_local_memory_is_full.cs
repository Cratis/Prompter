// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_Admission.when_checking;

public class and_local_memory_is_full : Specification
{
    Admission _guard = null!;
    AdmissionDecision _result = null!;

    void Establish()
    {
        var options = Options.Create(new PrompterOptions());
        options.Value.Protection.AllowedGuilds = [123];
        options.Value.Protection.LocalCapacity = 2;
        _guard = new(options, new RateLimiter(options), TimeProvider.System);
        _guard.TryEnter(new(123, 1, 1), "first");
    }

    void Because() => _result = _guard.TryEnter(new(123, 2, 2), "second");

    [Fact] void should_fail_closed_instead_of_growing_without_bound() => _result.Allowed.ShouldBeFalse();
}
