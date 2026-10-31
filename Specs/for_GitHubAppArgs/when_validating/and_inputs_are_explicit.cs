// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Services;

namespace Cratis.Prompter.Specs.for_GitHubAppArgs.when_validating;

public class and_inputs_are_explicit
{
    public static GitHubAppArgs Approved => new()
    {
        AppId = "123", InstallationId = "456", Owner = "Cratis", Repositories = "Cratis/Arc",
        ExternalSecretName = "approved-app-key", KeyReference = "signing.pem"
    };

    [Fact] void should_accept_inert_defaults() => new GitHubAppArgs().IsValid.ShouldBeTrue();
    [Fact] void should_accept_complete_identity() => Approved.IsValid.ShouldBeTrue();

    [Fact]
    void should_reject_partial_and_hostile_inputs_without_echoing_them()
    {
        foreach (var args in new[]
        {
            new GitHubAppArgs { AppId = "123" }, Approved with { AppId = "-1" }, Approved with { AppId = "+1" },
            Approved with { InstallationId = "0" }, Approved with { AppId = "9223372036854775808" },
            Approved with { Owner = "../Cratis" }, Approved with { Repositories = "Other/Arc" },
            Approved with { Repositories = "Cratis/*" }, Approved with { Repositories = "Cratis/Arc,cratis/arc" },
            Approved with { Repositories = "Cratis/Arc," }, Approved with { Repositories = "Cratis/../Arc" },
            Approved with { ExternalSecretName = "../key" }, Approved with { KeyReference = "../key.pem" },
            Approved with { ExternalSecretName = string.Empty }, Approved with { KeyReference = string.Empty }
        })
        {
            args.IsValid.ShouldBeFalse();
            var error = Catch.Exception(() => GitHubAppEnvironment.Values(args));
            error.ShouldBeOfExactType<InvalidGitHubApp>();
            error!.Message.ShouldEqual("GitHub App deployment configuration is not approved.");
        }
    }
}
