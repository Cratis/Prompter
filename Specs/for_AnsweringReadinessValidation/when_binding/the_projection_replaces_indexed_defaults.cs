// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_AnsweringReadinessValidation.when_binding;

public class the_projection_replaces_indexed_defaults : given.configured_answering
{
    PrompterOptions _options = null!;
    IReadOnlyDictionary<string, string> _projection = null!;
    int _indexedEntries;

    void Because()
    {
        Configuration["Cratis:Prompter:Protection:AllowedGuilds:0"] = "111";
        Configuration["Cratis:Prompter:Protection:AllowedGuilds:1"] = "222";
        _projection = AnsweringReadinessEnvironment.Values(new()
        {
            AnsweringEnabled = true,
            AnswerAccount = "secondary",
            AnswerAllowSecondary = true,
            AllowedGuilds = [AnsweringReadinessValidation.ApprovedGuild]
        });
        using var host = Build(_projection);
        host.Services.GetRequiredService<IStartupValidator>().Validate();
        _options = host.Services.GetRequiredService<IOptions<PrompterOptions>>().Value;
        _indexedEntries = host.Services.GetRequiredService<IConfiguration>().GetSection("Cratis:Prompter:Protection:AllowedGuilds").GetChildren().Count();
    }

    [Fact] void should_emit_the_approved_indexed_guild() => _projection["Cratis__Prompter__Protection__AllowedGuilds__0"].ShouldEqual(AnsweringReadinessValidation.ApprovedGuild);
    [Fact] void should_emit_the_authoritative_replacement() => _projection["Cratis__Prompter__Protection__ConfiguredAllowedGuilds"].ShouldEqual(AnsweringReadinessValidation.ApprovedGuild);
    [Fact] void should_exercise_a_lower_priority_index_that_still_exists() => _indexedEntries.ShouldEqual(2);
    [Fact] void should_keep_only_the_approved_guild_in_effective_options() => _options.Protection.AllowedGuilds.ShouldEqual<ulong>([1182595891576717413UL]);
    [Fact] void should_keep_indexing_disabled() => _options.Protection.IndexingEnabled.ShouldBeFalse();
    [Fact] void should_keep_the_shared_daily_cap_at_five_dollars() => _options.Protection.DailyUsd.ShouldEqual(5m);
    [Fact] void should_keep_issue_draft_secondary_disabled() => _options.Anthropic.IssueDraft.AllowSecondary.ShouldBeFalse();
}
