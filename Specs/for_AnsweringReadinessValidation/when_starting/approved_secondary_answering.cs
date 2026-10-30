// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_AnsweringReadinessValidation.when_starting;

public class approved_secondary_answering : given.configured_answering
{
    Exception? _failure;
    PrompterOptions _options = null!;

    void Because()
    {
        using var host = Build();
        _failure = Catch.Exception(() => host.Services.GetRequiredService<IStartupValidator>().Validate());
        _options = host.Services.GetRequiredService<IOptions<PrompterOptions>>().Value;
    }

    [Fact] void should_pass_startup_validation_without_provider_calls() => _failure.ShouldBeNull();
    [Fact] void should_not_require_a_primary_key_for_explicit_secondary_selection() => _options.Anthropic.ApiKey.ShouldBeEmpty();
    [Fact] void should_leave_indexing_off() => _options.Protection.IndexingEnabled.ShouldBeFalse();
    [Fact] void should_not_enable_secondary_for_issue_drafts() => _options.Anthropic.IssueDraft.AllowSecondary.ShouldBeFalse();
    [Fact] void should_not_fabricate_quota_state() => _options.Anthropic.PrimaryQuotaResetAt.ShouldBeNull();
}
