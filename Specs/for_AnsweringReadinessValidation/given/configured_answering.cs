// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Cratis.Prompter.Specs.for_AnsweringReadinessValidation.given;

public class configured_answering : Specification
{
    protected IDictionary<string, string?> Configuration { get; private set; } = null!;

    protected IHost Build(IReadOnlyDictionary<string, string>? projection = null)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Configuration.AddInMemoryCollection(Configuration);
        if (projection is not null)
        {
            builder.Configuration.AddInMemoryCollection(projection.Select(pair => KeyValuePair.Create<string, string?>(pair.Key.Replace("__", ":", StringComparison.Ordinal), pair.Value)));
        }

        builder.AddPrompter();
        return builder.Build();
    }

    void Establish()
    {
        Configuration = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["Cratis:Prompter:Protection:AnsweringEnabled"] = "true",
            ["Cratis:Prompter:Protection:IndexingEnabled"] = "false",
            [AnsweringReadinessValidation.GuildConfigurationKey] = AnsweringReadinessValidation.ApprovedGuild.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["Cratis:Prompter:Anthropic:Answer:PreferredAccount"] = "secondary",
            ["Cratis:Prompter:Anthropic:Answer:AllowSecondary"] = "true",
            ["Cratis:Prompter:Anthropic:SecondaryApiKey"] = "synthetic-secondary-key"
        };
    }
}
