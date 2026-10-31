// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub.Authentication;
using Cratis.Prompter.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_GitHubRepositoryConfiguration.when_resolving;

public class and_configuration_is_layered
{
    const string Root = "Cratis:Prompter:GitHub:";

    [Theory]
    [InlineData("[\"Cratis/Arc\"]", 1)]
    [InlineData("[]", 0)]
    void should_replace_inherited_indexes(string value, int count)
    {
        var configuration = Configuration(value);
        var options = Resolve(configuration);
        configuration[Root + "InstallationRepositories:1"].ShouldEqual("Cratis/Chronicle");
        options.GitHub.InstallationRepositories.Count.ShouldEqual(count);
        if (count > 0) options.GitHub.InstallationRepositories[0].ShouldEqual("Cratis/Arc");
    }

    [Fact]
    void should_preserve_legacy_configuration_if_absent() => Resolve(Configuration(null)).GitHub.InstallationRepositories.Count.ShouldEqual(2);

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[")]
    [InlineData("[null]")]
    [InlineData("[42]")]
    void should_reject_invalid_values_without_fallback(string value)
    {
        var error = Catch.Exception(() => Resolve(Configuration(value)));
        error.ShouldBeOfExactType<GitHubAuthorizationUnavailable>();
        error!.Message.ShouldEqual("GitHub authorization is unavailable.");
    }

    [Fact]
    void should_reject_a_present_null_scalar()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [Root + "InstallationRepositoriesJson"] = null,
            [Root + "AllowTokenFallback"] = "true"
        }).Build();
        Catch.Exception(() => Resolve(configuration)).ShouldBeOfExactType<GitHubAuthorizationUnavailable>();
    }

    [Fact]
    void should_reject_child_only_configuration()
    {
        var configuration = Configuration(null);
        configuration[Root + "InstallationRepositoriesJson:0"] = "Cratis/Arc";
        Catch.Exception(() => Resolve(configuration)).ShouldBeOfExactType<GitHubAuthorizationUnavailable>();
    }

    IConfigurationRoot Configuration(string? value)
    {
        var builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [Root + "InstallationRepositories:0"] = "Cratis/Arc",
            [Root + "InstallationRepositories:1"] = "Cratis/Chronicle",
            [Root + "AllowTokenFallback"] = "true"
        });
        if (value is not null) builder.AddInMemoryCollection(new Dictionary<string, string?> { [Root + "InstallationRepositoriesJson"] = value });
        return builder.Build();
    }

    PrompterOptions Resolve(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddOptions<PrompterOptions>().Bind(configuration.GetSection("Cratis:Prompter"))
            .PostConfigure(options => GitHubRepositoryConfiguration.Apply(configuration, options));
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<PrompterOptions>>().Value;
    }
}
