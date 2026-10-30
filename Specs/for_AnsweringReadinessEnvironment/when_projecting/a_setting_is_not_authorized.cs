// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Services;

namespace Cratis.Prompter.Specs.for_AnsweringReadinessEnvironment.when_projecting;

public class a_setting_is_not_authorized : Specification
{
    [Theory]
    [InlineData("empty-guilds")]
    [InlineData("other-guild")]
    [InlineData("mixed-guilds")]
    [InlineData("answer-account")]
    [InlineData("issue-account")]
    [InlineData("account-case")]
    [InlineData("answer-opt-in")]
    [InlineData("issue-opt-in")]
    public void should_reject_before_projecting_a_workload(string invalid)
    {
        var valid = new AnsweringReadinessArgs
        {
            AnsweringEnabled = true,
            AnswerAccount = "secondary",
            AnswerAllowSecondary = true,
            AllowedGuilds = [AnsweringReadinessValidation.ApprovedGuild]
        };
        var mutations = new Dictionary<string, AnsweringReadinessArgs>(StringComparer.Ordinal)
        {
            ["empty-guilds"] = valid with { AllowedGuilds = [] },
            ["other-guild"] = valid with { AllowedGuilds = ["111"] },
            ["mixed-guilds"] = valid with { AllowedGuilds = [AnsweringReadinessValidation.ApprovedGuild, "111"] },
            ["answer-account"] = valid with { AnswerAccount = "unknown" },
            ["issue-account"] = valid with { IssueDraftAccount = "unknown" },
            ["account-case"] = valid with { AnswerAccount = "Secondary" },
            ["answer-opt-in"] = valid with { AnswerAllowSecondary = false },
            ["issue-opt-in"] = valid with { IssueDraftAccount = "secondary" }
        };
        Catch.Exception(() => AnsweringReadinessEnvironment.Values(mutations[invalid])).ShouldBeOfExactType<InvalidAnsweringReadiness>();
    }
}
