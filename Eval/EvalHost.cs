// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Anthropic;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Embeddings;
using Cratis.Prompter.Retrieval;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Cratis.Prompter.Eval;

/// <summary>
/// Builds the host for the eval harness. It reuses the bot's own service registrations - the same Postgres
/// data source, Voyage embeddings, Anthropic chat client, hybrid retrieval and <see cref="IAnswers"/>
/// pipeline the production bot runs - minus Discord, so the golden set is scored against the real corpus.
/// </summary>
internal static class EvalHost
{
    /// <summary>
    /// Builds the configured host. Configuration binds to <c>Cratis:Prompter</c> (env vars
    /// <c>Cratis__Prompter__…</c>), so the Voyage and Anthropic API keys are supplied the same way as for the
    /// bot.
    /// </summary>
    /// <param name="args">The process arguments (forwarded to configuration).</param>
    /// <returns>The built host.</returns>
    public static IHost Build(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        var configSection = ConfigurationPath.Combine("Cratis", "Prompter");

        builder.Services
            .AddOptions<PrompterOptions>()
            .BindConfiguration(configSection);

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        builder.Services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<PrompterOptions>>().Value;
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(options.ConnectionString);
            dataSourceBuilder.UseVector();

            return dataSourceBuilder.Build();
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

        return builder.Build();
    }
}
