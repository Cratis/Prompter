// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_AnsweringReadinessValidation.when_starting;

public class answering_is_disabled : given.configured_answering
{
    Exception? _failure;
    PrompterOptions _options = null!;

    void Because()
    {
        Configuration["Cratis:Prompter:Protection:AnsweringEnabled"] = "false";
        Configuration["Cratis:Prompter:Protection:ConfiguredAllowedGuilds"] = null;
        Configuration["Cratis:Prompter:Anthropic:SecondaryApiKey"] = null;
        Configuration["Cratis:Prompter:Protection:AllowedGuilds:0"] = "111";
        using var host = Build();
        _failure = Catch.Exception(() => host.Services.GetRequiredService<IStartupValidator>().Validate());
        _options = host.Services.GetRequiredService<IOptions<PrompterOptions>>().Value;
    }

    [Fact] void should_allow_disabled_work_without_a_secondary_key() => _failure.ShouldBeNull();
    [Fact] void should_not_inherit_any_default_or_indexed_guilds() => _options.Protection.AllowedGuilds.ShouldBeEmpty();
}
