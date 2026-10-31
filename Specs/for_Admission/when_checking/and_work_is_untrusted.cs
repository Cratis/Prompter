// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_Admission.when_checking;

public class and_work_is_untrusted : Specification
{
    Admission _admission = null!;
    PrompterOptions _options = null!;

    void Establish()
    {
        _options = new();
        _options.Protection.AllowedGuilds = [123];
        _options.Protection.DeniedChannels = [456];
        var options = Options.Create(_options);
        _admission = new(options, new RateLimiter(options), TimeProvider.System);
    }

    [Theory]
    [InlineData(null, 1, "hello")]
    [InlineData(999UL, 1, "hello")]
    [InlineData(123UL, 456, "hello")]
    void should_reject_outside_scope(ulong? guild, ulong channel, string question) =>
        _admission.TryEnter(new(guild, channel, 1), question).Allowed.ShouldBeFalse();

    [Fact] void should_reject_oversized_input() =>
        _admission.TryEnter(new(123, 1, 1), new string('a', 4001)).Allowed.ShouldBeFalse();

    [Fact] void should_honor_the_answering_switch()
    {
        _options.Protection.AnsweringEnabled = false;
        _admission.TryEnter(new(123, 1, 1), "hello").Allowed.ShouldBeFalse();
    }
}
