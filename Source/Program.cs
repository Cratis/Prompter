// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Cli;
using Cratis.Prompter.Hosting;
using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Operations;
using Cratis.Prompter.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;

var mode = args.FirstOrDefault() ?? "bot";

if (mode == "index" || mode == "ask")
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.AddPrompter();

    var host = builder.Build();
    await host.Services.GetRequiredService<IChunks>().EnsureSchema();

    if (mode == "index")
    {
        var run = await host.Services.GetRequiredService<IIndexer>().Run();
        Console.WriteLine(
            $"Indexed {run.Pages} pages in {run.Duration:mm\\:ss}: " +
            $"{run.Embedded} embedded, {run.Unchanged} unchanged, {run.Removed} removed.");
    }
    else
    {
        var ask = AskArguments.Parse(args.Skip(1));
        var answer = await host.Services.GetRequiredService<IAnswers>().For(new(ask.Question), "cli");
        foreach (var line in AskOutput.Lines(answer, ask.Verbose))
        {
            Console.WriteLine(line);
        }

        Environment.ExitCode = AskOutput.ExitCode(answer);
    }

    return;
}

// Bot mode runs as a web application: Kestrel co-hosts the Discord gateway client (a background hosted
// service dialing out to Discord) with the operational HTTP endpoints (health probe + re-index webhook).
var configSection = ConfigurationPath.Combine("Cratis", "Prompter");
var webBuilder = WebApplication.CreateBuilder(args);

webBuilder.AddPrompter();

// Bot mode is the only run mode that opens a Discord gateway, so the token requirement is validated here
// rather than in the shared registration (the console index/ask modes run without a token). This validator
// lives only in the bot process and fails startup fast on an empty token instead of surfacing later as an
// opaque gateway authentication failure.
webBuilder.Services
    .AddOptions<Cratis.Prompter.PrompterOptions>()
    .Validate(
        options => options.Discord.TokenIsPresent,
        "Cratis:Prompter:Discord:Token must be set in bot mode; an empty token fails late at the Discord gateway rather than at startup.")
    .ValidateOnStart();

webBuilder.Services.AddSingleton<ReindexGate>();
webBuilder.Services.AddHostedService<RetentionPurge>();
webBuilder.Services
    .AddDiscordGateway(options =>
    {
        options.Token = webBuilder.Configuration[ConfigurationPath.Combine(configSection, "Discord", "Token")];
        options.Intents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent;
    })
    .AddGatewayHandlers(typeof(Program).Assembly)
    .AddApplicationCommands()
    .AddComponentInteractions();

var app = webBuilder.Build();

await app.Services.GetRequiredService<IChunks>().EnsureSchema();

app.AddModules(typeof(Program).Assembly);
app.MapPrompterEndpoints();

await app.RunAsync();
