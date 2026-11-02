// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Specs.for_HostBuilderExtensions.when_composing;

public class and_a_crawl_limit_is_invalid : Specification
{
    IHost _host = null!;
    Exception? _error;

    void Establish()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Cratis:Prompter:Ingestion:MaxCrawlPages"] = "0" });
        builder.AddPrompter();
        _host = builder.Build();
    }

    void Because() => _error = Catch.Exception(() => _ = _host.Services.GetRequiredService<IOptions<PrompterOptions>>().Value);

    void Destroy() => _host.Dispose();

    [Fact] void should_reject_the_configuration() => _error.ShouldBeOfExactType<OptionsValidationException>();
}
