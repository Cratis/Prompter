// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_GitHubWriteGate.when_writing;

public class and_writes_are_disabled : Specification
{
    Exception? _error;

    async Task Because()
    {
        var options = Options.Create(new PrompterOptions());
        options.Value.Protection.GitHubWritesEnabled = false;

        // No inner transport: reaching dispatch would fail with a different exception, not WorkRefused.
        using var gate = new GitHubWriteGate(options);
        using var client = new HttpClient(gate, disposeHandler: false);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://example.invalid/issues");
        _error = await Catch.Exception(async () =>
        {
            using var response = await client.SendAsync(request);
        });
    }

    [Fact] void should_refuse_without_reaching_a_transport() => _error.ShouldBeOfExactType<WorkRefused>();
}
