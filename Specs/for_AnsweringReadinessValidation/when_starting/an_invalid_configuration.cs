// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_AnsweringReadinessValidation.when_starting;

public class an_invalid_configuration : given.configured_answering
{
    [Theory]
    [InlineData("Protection:ConfiguredAllowedGuilds", null)]
    [InlineData("Protection:ConfiguredAllowedGuilds", "")]
    [InlineData("Protection:ConfiguredAllowedGuilds", "111")]
    [InlineData("Protection:ConfiguredAllowedGuilds", "not-a-guild")]
    [InlineData("Protection:ConfiguredAllowedGuilds", "1182595891576717413,111")]
    [InlineData("Anthropic:Answer:PreferredAccount", "unknown")]
    [InlineData("Anthropic:Answer:PreferredAccount", "Secondary")]
    [InlineData("Anthropic:IssueDraft:PreferredAccount", "unknown")]
    [InlineData("Anthropic:IssueDraft:PreferredAccount", "secondary")]
    [InlineData("Anthropic:Answer:AllowSecondary", "false")]
    [InlineData("Anthropic:SecondaryApiKey", null)]
    [InlineData("Anthropic:SecondaryApiKey", "")]
    [InlineData("Anthropic:SecondaryApiKey", "   ")]
    public void should_fail_startup_before_reporting_healthy(string key, string? value)
    {
        Configuration["Cratis:Prompter:" + key] = value;
        using var host = Build();
        var failure = Catch.Exception(() => host.Services.GetRequiredService<IStartupValidator>().Validate());
        failure.ShouldBeOfExactType<OptionsValidationException>();
        failure!.Message.Contains("synthetic-secondary-key", StringComparison.Ordinal).ShouldBeFalse();
    }
}
