// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Hosting;
using Cratis.Prompter.Ingestion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Prompter.Specs.for_HostBuilderExtensions.when_composing;

public class and_the_docs_transport_is_resolved : Specification
{
    IHost _host = null!;
    SocketsHttpHandler _transport = null!;

    void Establish()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.AddPrompter();
        _host = builder.Build();
    }

    void Because()
    {
        var handler = _host.Services.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(nameof(DocsSite));
        while (handler is DelegatingHandler delegating) handler = delegating.InnerHandler!;
        _transport = (SocketsHttpHandler)handler;
    }

    void Destroy() => _host.Dispose();

    [Fact] void should_require_explicit_redirect_validation() => _transport.AllowAutoRedirect.ShouldBeFalse();
    [Fact] void should_not_retain_source_cookies() => _transport.UseCookies.ShouldBeFalse();
}
