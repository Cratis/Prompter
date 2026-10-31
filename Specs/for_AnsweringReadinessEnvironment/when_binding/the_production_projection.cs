// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Deployment.Services;
using Microsoft.Extensions.Configuration;

namespace Cratis.Prompter.Specs.for_AnsweringReadinessEnvironment.when_binding;

public class the_production_projection : Specification
{
    PrompterOptions _defaults = null!;
    PrompterOptions _secondary = null!;
    PrompterOptions _trial = null!;

    static PrompterOptions Bind(AnsweringReadinessArgs args)
    {
        var values = AnsweringReadinessEnvironment.Values(args).Select(pair => KeyValuePair.Create<string, string?>(pair.Key.Replace("__", ":", StringComparison.Ordinal), pair.Value));
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build().GetSection("Cratis:Prompter").Get<PrompterOptions>()!;
    }

    void Because()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            _defaults = Bind(new());
            _trial = Bind(new() { MinSemanticSimilarity = 0.4 });
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }

        _secondary = Bind(new() { AnsweringEnabled = true, AnswerAccount = "secondary", AnswerAllowSecondary = true, AllowedGuilds = [AnsweringReadinessValidation.ApprovedGuild] });
    }

    [Fact] void should_leave_both_paid_workloads_disabled() => (_defaults.Protection.AnsweringEnabled || _defaults.Protection.IndexingEnabled).ShouldBeFalse();
    [Fact] void should_price_the_verified_chat_model() => _defaults.Protection.Anthropic.Covers(_defaults.Anthropic.Model, true).ShouldBeTrue();
    [Fact] void should_reserve_the_worst_cache_write_rate() => _defaults.Protection.Anthropic.InputUsdPerMillion.ShouldEqual(4m);
    [Fact] void should_cover_hidden_thinking_in_the_output_bound() => _defaults.Protection.Anthropic.OutputUsdPerMillion.ShouldEqual(10m);
    [Fact] void should_price_the_verified_embedding_model() => _defaults.Protection.Voyage.Covers(_defaults.Voyage.Model, false).ShouldBeTrue();
    [Fact] void should_bound_the_global_ceiling_at_five_dollars() => _defaults.Protection.DailyUsd.ShouldEqual(5m);
    [Fact] void should_enforce_a_local_input_ceiling_below_both_vendor_limits() => _defaults.Protection.Anthropic.MaxInputTokens.ShouldEqual(32000);
    [Fact] void should_bind_the_explicit_initial_semantic_policy() => _defaults.Answering.MinSemanticSimilarity.ShouldEqual(0.5);
    [Fact] void should_bind_the_production_trial_independently_of_process_culture() => _trial.Answering.MinSemanticSimilarity.ShouldEqual(0.4);
    [Fact] void should_admit_the_observed_positive_similarity_for_retrieval_only() => (_trial.Answering.MinSemanticSimilarity <= 0.437856).ShouldBeTrue();
    [Fact] void should_reject_the_observed_negative_similarity() => (_trial.Answering.MinSemanticSimilarity <= 0.354734).ShouldBeFalse();
    [Fact] void should_bind_the_explicit_secondary_opt_in() => AccountSelection.Select(_secondary.Anthropic.Answer, null, true, DateTimeOffset.UnixEpoch).ShouldEqual("secondary");
    [Fact] void should_not_enable_issue_draft_secondary_by_answer_opt_in() => _secondary.Anthropic.IssueDraft.AllowSecondary.ShouldBeFalse();
    [Fact] void should_not_project_secrets_or_manufacture_a_quota_reset() => AnsweringReadinessEnvironment.Values(new()).Keys.Any(key => key.EndsWith("ApiKey", StringComparison.Ordinal) || key.EndsWith("ResetAt", StringComparison.Ordinal)).ShouldBeFalse();
}
