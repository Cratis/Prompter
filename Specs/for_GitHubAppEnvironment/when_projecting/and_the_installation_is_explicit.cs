// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Services;
using Cratis.Prompter.Hosting;
using Microsoft.Extensions.Configuration;

namespace Cratis.Prompter.Specs.for_GitHubAppEnvironment.when_projecting;

public class and_the_installation_is_explicit
{
    [Fact] void should_leave_absent_app_environment_untouched() => GitHubAppEnvironment.Values(new()).Count.ShouldEqual(0);

    [Fact]
    void should_bind_exactly_the_deployment_identity_and_force_writes_and_fallback_off()
    {
        var args = for_GitHubAppArgs.when_validating.and_inputs_are_explicit.Approved;
        var values = GitHubAppEnvironment.Values(args);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Prompter:GitHub:InstallationRepositories:1"] = "Cratis/Chronicle",
            ["Cratis:Prompter:GitHub:AllowTokenFallback"] = "true",
            ["Cratis:Prompter:Protection:GitHubWritesEnabled"] = "true"
        }).AddInMemoryCollection(values.Select(pair => KeyValuePair.Create<string, string?>(pair.Key.Replace("__", ":", StringComparison.Ordinal), pair.Value))).Build();
        var options = configuration.GetSection("Cratis:Prompter").Get<PrompterOptions>()!;
        GitHubRepositoryConfiguration.Apply(configuration, options);
        options.GitHub.AppId.ShouldEqual(123);
        options.GitHub.InstallationId.ShouldEqual(456);
        options.GitHub.Owner.ShouldEqual("Cratis");
        options.GitHub.ApiUrl.ShouldEqual("https://api.github.com/");
        options.GitHub.AppPrivateKeyPath.ShouldEqual(GitHubAppEnvironment.KeyPath);
        options.GitHub.InstallationRepositories.Count.ShouldEqual(1);
        options.GitHub.InstallationRepositories[0].ShouldEqual("Cratis/Arc");
        options.GitHub.AllowTokenFallback.ShouldBeFalse();
        options.Protection.GitHubWritesEnabled.ShouldBeFalse();
        values.Keys.Any(key => key.Contains("InstallationRepositories__", StringComparison.Ordinal)).ShouldBeFalse();
    }
}
