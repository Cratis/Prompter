// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub.Authentication;
using Cratis.Prompter.Protection;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_GitHubAuthentication.when_authorizing_requests;

public class and_writes_are_disabled : Specification
{
    bool _refused;

    async Task Because()
    {
        // Deliberately unusable authorization: the outer gate must reject the write before minting.
        var options = Options.Create(new PrompterOptions { Protection = new() { GitHubWritesEnabled = false } });
        using var authentication = new GitHubAuthentication(new GitHubPolicy(options), null);
        using var gate = new GitHubWriteGate(options) { InnerHandler = authentication };
        using var client = new HttpClient(gate, disposeHandler: false);
        using var body = new StringContent("{}");
        try
        {
            using var response = await client.PostAsync("https://api.github.com/repos/Cratis/Chronicle/issues", body);
        }
        catch (WorkRefused)
        {
            _refused = true;
        }
    }

    [Fact] void should_keep_the_protected_gate_outside_authentication() => _refused.ShouldBeTrue();
    [Fact] void should_not_enable_filing_by_default() => new GitHubOptions().FilingEnabled.ShouldBeFalse();
    [Fact] void should_require_an_explicit_repository_list() => new GitHubOptions { AppId = 123, InstallationId = 456, AppPrivateKeyPath = "synthetic" }.FilingEnabled.ShouldBeFalse();
}
