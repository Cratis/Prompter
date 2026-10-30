// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Specs.Fakes;

namespace Cratis.Prompter.Specs.for_Admission.given;

public class an_admission : Specification
{
    protected Admission Guard { get; private set; } = null!;
    protected PrompterOptions Options { get; private set; } = null!;
    protected ControlledTime Clock { get; private set; } = null!;

    void Establish()
    {
        Options = new();
        Options.Protection.AllowedGuilds = [123];
        Clock = new(new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var config = Microsoft.Extensions.Options.Options.Create(Options);
        Guard = new(config, new RateLimiter(config), Clock);
    }
}
