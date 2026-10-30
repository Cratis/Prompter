// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Embeddings;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Retrieval;
using Cratis.Prompter.Storage;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Cratis.Prompter.Hosting;

/// <summary>
/// Registers Prompter's shared services onto a host builder, so the console modes (<c language="csharp">index</c>, <c language="csharp">ask</c>)
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
            .ValidateAnsweringReadiness(builder.Configuration)
            .Validate(
                options => options.Discord.RateLimit.IsValid,
                "Cratis:Prompter:Discord:RateLimit requires WindowMinutes > 0 and MaxQuestions >= 0.")
            .Validate(
                options => options.RetentionIsValid,
                "Cratis:Prompter:RetentionDays must be greater than 0; a value of 0 purges the entire interactions table on the first sweep.")
            .Validate(
                options => options.Voyage.DimensionsMatchSchema,
                $"Cratis:Prompter:Voyage:Dimensions must equal {VoyageOptions.SchemaDimensions} to match the fixed 'embedding vector({VoyageOptions.SchemaDimensions})' schema column; any other value fails every embedding upsert at runtime.")
            .Validate(
                options => options.Discord.AnswerTimeoutIsValid,
                "Cratis:Prompter:Discord:AnswerTimeoutSeconds must be greater than 0; a value of 0 abandons every answer immediately and replies with the apology instead.")
            .Validate(options => options.Protection.IsValid, "Cratis:Prompter:Protection requires positive bounds and nonnegative allowances.")
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

        AddModels(builder);

        // GitHub: filing issues from Discord and answering newly-opened ones. The client is registered
        // regardless of whether a token is configured - the command checks GitHubOptions.FilingEnabled and
        // says so, which is a better failure than an unresolvable dependency at startup.
        builder.Services.AddHttpClient<IIssues, Issues>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<PrompterOptions>>().Value;
            client.BaseAddress = new Uri(options.GitHub.ApiUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Authorization = new("Bearer", options.GitHub.Token);
            client.DefaultRequestHeaders.Accept.Add(new("application/vnd.github+json"));

            // GitHub rejects requests without a user agent, and names the caller in rate-limit responses.
            client.DefaultRequestHeaders.UserAgent.Add(new("Cratis-Prompter", "1.0"));
        }).AddHttpMessageHandler<GitHubWriteGate>();

        builder.Services.AddTransient<GitHubWriteGate>();
        builder.Services.AddSingleton<IssueDrafting>();
        builder.Services.AddSingleton<IIssueDrafting, ProtectedDrafting>();
        builder.Services.AddSingleton<PendingIssues>();

        builder.Services.AddSingleton<IChunks, Chunks>();
        builder.Services.AddSingleton<IInteractionLog, InteractionLog>();
        builder.Services.AddSingleton<IPassages, Passages>();
        builder.Services.AddSingleton<Answers>();
        builder.Services.AddSingleton<IAnswers, ProtectedAnswers>();
        builder.Services.AddSingleton<Indexer>();
        builder.Services.AddSingleton<ProtectedIndexer>();
        builder.Services.AddSingleton<IIndexer>(sp => new ObservedIndexer(sp.GetRequiredService<ProtectedIndexer>(), sp.GetRequiredService<IIndexHistory>()));
        builder.Services.AddSingleton<ISpendLedger, SpendLedger>();
        builder.Services.AddSingleton<PaidWork>();
        builder.Services.AddSingleton<Admission>();

        // The per-user question throttle and the clock its refills are measured against. Both are shared
        // across every Discord entry point, so they live as singletons in the common registration.
        builder.Services.Configure<MetricsOptions>(builder.Configuration.GetSection("Cratis:Prompter:Metrics"));
        builder.Services.AddSingleton<MetricsExport>();
        if (builder.Configuration.GetValue<bool>("Cratis:Prompter:Metrics:Enabled")) builder.Services.AddHostedService<DurableMetrics>();
        builder.Services.AddSingleton<IIndexHistory, IndexHistory>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<Discord.RateLimiter>();
        builder.Services.AddSingleton<Discord.IConversationHistory, Discord.DiscordConversationHistory>();
        builder.Services.AddSingleton<Discord.ConversationContextReader>();
    }

    static void AddModels(IHostApplicationBuilder builder)
    {
        builder.Services.AddProviderAccounting();
        builder.Services.AddHttpClient<VoyageEmbeddings>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<PrompterOptions>>().Value;
            client.BaseAddress = new Uri(options.Voyage.Url);
            client.DefaultRequestHeaders.Authorization = new("Bearer", options.Voyage.ApiKey);
        });

        builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
            new ResilientEmbeddingGenerator(
                new BudgetedEmbeddings(
                    sp.GetRequiredService<VoyageEmbeddings>(),
                    sp.GetRequiredService<ISpendLedger>(),
                    sp.GetRequiredService<IOptions<PrompterOptions>>()),
                sp.GetRequiredService<IOptions<PrompterOptions>>(),
                sp.GetRequiredService<ILogger<ResilientEmbeddingGenerator>>(),
                sp.GetRequiredService<IProviderAccounting>()));

        builder.Services.AddSingleton<IChatClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<PrompterOptions>>().Value;
            var configuration = sp.GetRequiredService<IOptions<PrompterOptions>>();
            var primary = new BudgetedChat(new AnthropicConnection(options.Anthropic.ApiKey), configuration);
            var secondary = string.IsNullOrWhiteSpace(options.Anthropic.SecondaryApiKey)
                ? null : new BudgetedChat(new AnthropicConnection(options.Anthropic.SecondaryApiKey), configuration);

            return new AnthropicAccounts(primary, secondary, options.Anthropic, sp.GetRequiredService<IProviderAccounting>(), options.Protection.Anthropic);
        });
    }
}
