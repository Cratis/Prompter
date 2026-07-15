// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Anthropic;
using Cratis.Prompter;
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
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using Npgsql;

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

var mode = args.FirstOrDefault() ?? "bot";

if (mode == "bot")
{
    builder.Services
        .AddDiscordGateway(options =>
        {
            options.Token = builder.Configuration[ConfigurationPath.Combine(configSection, "Discord", "Token")];
            options.Intents = GatewayIntents.GuildMessages | GatewayIntents.MessageContent;
        })
        .AddGatewayHandlers(typeof(Program).Assembly)
        .AddApplicationCommands();
}

var host = builder.Build();

await host.Services.GetRequiredService<IChunks>().EnsureSchema();

switch (mode)
{
    case "index":
        var run = await host.Services.GetRequiredService<IIndexer>().Run();
        Console.WriteLine(
            $"Indexed {run.Pages} pages in {run.Duration:mm\\:ss}: " +
            $"{run.Embedded} embedded, {run.Unchanged} unchanged, {run.Removed} removed.");
        break;

    case "ask":
        var question = string.Join(' ', args.Skip(1));
        var answer = await host.Services.GetRequiredService<IAnswers>().For(new(question), "cli", "cli");
        Console.WriteLine(answer.Text);
        foreach (var citation in answer.Citations)
        {
            Console.WriteLine($"  - {citation}");
        }

        break;

    default:
        host.AddModules(typeof(Program).Assembly);
        await host.RunAsync();
        break;
}
