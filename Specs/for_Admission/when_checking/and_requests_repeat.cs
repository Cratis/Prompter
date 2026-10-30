// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_Admission.when_checking;

public class and_requests_repeat : Specification
{
    Admission _admission = null!;
    AdmissionDecision _duplicate = null!;
    AdmissionDecision _differentUser = null!;
    AdmissionDecision _expired = null!;

    void Establish()
    {
        var options = Options.Create(new PrompterOptions());
        options.Value.Protection.AllowedGuilds = [123];
        var time = new ControlledTime(DateTimeOffset.UtcNow);
        _admission = new(options, new RateLimiter(options), time);
        _admission.TryEnter(new(123, 1, 1), "Hello   WORLD");
        _duplicate = _admission.TryEnter(new(123, 1, 1), "hello world");
        _differentUser = _admission.TryEnter(new(123, 1, 2), "hello world");
        time.Advance(TimeSpan.FromMinutes(3));
    }

    void Because() => _expired = _admission.TryEnter(new(123, 1, 1), "hello world");

    [Fact] void should_suppress_normalized_duplicates() => _duplicate.Allowed.ShouldBeFalse();
    [Fact] void should_not_suppress_another_person() => _differentUser.Allowed.ShouldBeTrue();
    [Fact] void should_expire_the_duplicate() => _expired.Allowed.ShouldBeTrue();
}
