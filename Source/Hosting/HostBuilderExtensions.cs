// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Anthropic;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Embeddings;
using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Retrieval;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Cratis.Prompter.Hosting;

/// <summary>
/// Registers Prompter's shared services onto a host builder, so the console modes (<c>index</c>, <c>ask</c>)
/// and the bot's web application compose from exactly the same configuration and dependency graph.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Binds configuration and registers every service Prompter needs regardless of run mode.
    /// </summary>
    /// <param name="builder">The host application builder to configure.</param>
    public static void AddPrompter(this IHostApplicationBuilder builder)
    {
        var configSection = ConfigurationPath.Combine("Cratis", "Prompter");

        builder.Services
            .AddOptions<PrompterOptions>()
            .BindConfiguration(configSection)
            .Validate(
                options => options.Discord.RateLimit.IsValid,
                "Cratis:Prompter:Discord:RateLimit requires WindowMinutes > 0 and MaxQuestions >= 0.")
            .Validate(
                options => options.RetentionIsValid,
                "Cratis:Prompter:RetentionDays must be greater than 0; a value of 0 purges the entire interactions table on the first sweep.")
            .ValidateOnStart();

        builder.Services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<PrompterOptions>>().Value;
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(options.ConnectionString);
            dataSourceBuilder.UseVector();

            return dataSourceBuilder.Build();
        });

        builder.Services.AddHttpClient<IDocsSite, DocsSite>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<PrompterOptions>>().Value;
            client.BaseAddress = new Uri(options.DocsSiteUrl.TrimEnd('/') + "/");
        });

        builder.Services.AddHttpClient<VoyageEmbeddings>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<PrompterOptions>>().Value;
            client.BaseAddress = new Uri(options.Voyage.Url);
            client.DefaultRequestHeaders.Authorization = new("Bearer", options.Voyage.ApiKey);
        });

        builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
            new ResilientEmbeddingGenerator(
                sp.GetRequiredService<VoyageEmbeddings>(),
                sp.GetRequiredService<IOptions<PrompterOptions>>(),
                sp.GetRequiredService<ILogger<ResilientEmbeddingGenerator>>()));

        builder.Services.AddSingleton<IChatClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<PrompterOptions>>().Value;
            var client = options.Anthropic.ApiKey.Length > 0
                ? new AnthropicClient { ApiKey = options.Anthropic.ApiKey }
                : new AnthropicClient();

            return client.AsIChatClient(options.Anthropic.Model);
        });

        builder.Services.AddSingleton<IChunks, Chunks>();
        builder.Services.AddSingleton<IInteractionLog, InteractionLog>();
        builder.Services.AddSingleton<IPassages, Passages>();
        builder.Services.AddSingleton<IAnswers, Answers>();
        builder.Services.AddSingleton<IIndexer, Indexer>();

        // The per-user question throttle and the clock its refills are measured against. Both are shared
        // across every Discord entry point, so they live as singletons in the common registration.
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<Discord.RateLimiter>();
    }
}
