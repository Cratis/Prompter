// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Cli;
using Cratis.Prompter.Digest;
using Cratis.Prompter.Discord;
using Cratis.Prompter.Evaluation;
using Cratis.Prompter.Hosting;
using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Operations;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Storage;
using Cratis.Prompter.Telemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using Npgsql;

var mode = args.FirstOrDefault() ?? "bot";

if (mode == "eval")
{
    var evaluation = EvalArguments.Parse(args.Skip(1));
    var path = evaluation?.Set ?? EvalSet.DefaultPath(Directory.GetCurrentDirectory());
    if (evaluation is not { Paid: true } || path is null || !File.Exists(path))
    {
        await Console.Error.WriteLineAsync(EvalArguments.Usage);
        Environment.ExitCode = 2;
        return;
    }

    EvalSet set;
    try
    {
        set = EvalSet.Load(path);
    }
    catch (Exception error) when (error is IOException or YamlDotNet.Core.YamlException)
    {
        await Console.Error.WriteLineAsync(EvalArguments.Usage);
        Environment.ExitCode = 2;
        return;
    }

    var evalBuilder = Host.CreateApplicationBuilder([]);
    evalBuilder.AddPrompter();
    using var evalHost = evalBuilder.Build();
    _ = evalHost.Services.GetRequiredService<MetricsExport>();
    await evalHost.Services.GetRequiredService<IChunks>().EnsureSchema();
    if (await SavedPause.Load(evalHost.Services.GetRequiredService<AnsweringSwitch>(), evalHost.Services.GetRequiredService<IAnsweringPauses>()) is { } evalPaused)
    {
        await Console.Error.WriteLineAsync(evalPaused);
        Environment.ExitCode = 1;
        return;
    }

    var command = new EvalCommand(evalHost.Services.GetRequiredService<PaidWork>(), evalHost.Services.GetRequiredService<IAnswers>());
    var summary = await command.Run(set, evaluation);
    foreach (var line in summary.Lines()) Console.WriteLine(line);
    Environment.ExitCode = summary.ExitCode;
    return;
}

if (mode == "migrate")
{
    try
    {
        // Only the database is needed: the full host would also demand the Discord and model settings the init container does not have.
        var migrateConfiguration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var connectionString = migrateConfiguration[ConfigurationPath.Combine("Cratis", "Prompter", "ConnectionString")] ?? new PrompterOptions().ConnectionString;
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseVector();
        await using var dataSource = dataSourceBuilder.Build();
        await new Chunks(dataSource).EnsureSchema();

        var migration = migrateConfiguration.GetSection(ConfigurationPath.Combine("Cratis", "Prompter", "Migration")).Get<MigrationOptions>() ?? new();
        if (migration.ApplicationRole.Length == 0)
        {
            Console.WriteLine("Database schema current.");
        }
        else
        {
            if (migration.ApplicationRolePassword.Length == 0)
            {
                throw new InvalidOperationException("Cratis:Prompter:Migration:ApplicationRolePassword must be set when an application role is given.");
            }

            await ApplicationRole.Provision(dataSource, new DatabaseRoleName(migration.ApplicationRole), migration.ApplicationRolePassword);
            Console.WriteLine($"Database schema current; application role {migration.ApplicationRole} provisioned.");
        }
    }
    catch (Exception error)
    {
        // An exception escaping Main aborts PID 1 in the container, which surfaces as exit 139, not the cause.
        await Console.Error.WriteLineAsync(MigrateFailure.Message(error));
        Environment.ExitCode = MigrateFailure.ExitCode;
    }

    return;
}

if (mode == "index" || mode == "ask")
{
    try
    {
        var builder = Host.CreateApplicationBuilder(mode == "index" ? IndexArguments.ConfigurationArguments(args) : args);
        builder.AddPrompter();

        using var host = builder.Build();
        _ = host.Services.GetRequiredService<MetricsExport>();
        await host.Services.GetRequiredService<IChunks>().EnsureSchema();

        if (mode == "index")
        {
            var run = await host.Services.GetRequiredService<IIndexer>().Run(IndexArguments.Parse(args));
            Console.WriteLine(
                $"Indexed {run.Pages} pages in {run.Duration:mm\\:ss}: " +
                $"{run.Embedded} embedded, {run.Unchanged} unchanged, {run.Removed} removed.");
        }
        else
        {
            var ask = AskArguments.Parse(args.Skip(1));
            if (await SavedPause.Load(host.Services.GetRequiredService<AnsweringSwitch>(), host.Services.GetRequiredService<IAnsweringPauses>()) is { } askPaused)
            {
                await Console.Error.WriteLineAsync(askPaused);
                Environment.ExitCode = 1;
                return;
            }

            var answer = await host.Services.GetRequiredService<IAnswers>().For(new(ask.Question), "cli");
            foreach (var line in AskOutput.Lines(answer, ask.Verbose))
            {
                Console.WriteLine(line);
            }

            Environment.ExitCode = AskOutput.ExitCode(answer);
        }
    }
    catch (Exception error) when (mode == "index")
    {
        // An exception escaping Main aborts PID 1 in the container, which surfaces as exit 139, not the cause.
        await Console.Error.WriteLineAsync(IndexFailure.Message(error));
        Environment.ExitCode = IndexFailure.ExitCode;
    }

    return;
}

// Bot mode runs as a web application: Kestrel co-hosts the Discord gateway client (a background hosted
// service dialing out to Discord) with the operational HTTP endpoints (health probe + re-index webhook).
Environment.ExitCode = await BotFailure.Run(
    async () =>
    {
        var configSection = ConfigurationPath.Combine("Cratis", "Prompter");
        var webBuilder = WebApplication.CreateBuilder(args);

        webBuilder.AddPrompter();

        // Bot mode is the only run mode that opens a Discord gateway, so the token requirement is validated here
        // rather than in the shared registration (the console index/ask modes run without a token). This validator
        // lives only in the bot process and fails startup fast on an empty token instead of surfacing later as an
        // opaque gateway authentication failure.
        webBuilder.Services
            .AddOptions<PrompterOptions>()
            .Validate(
                options => options.Discord.TokenIsPresent,
                "Cratis:Prompter:Discord:Token must be set in bot mode; an empty token fails late at the Discord gateway rather than at startup.")
            .ValidateOnStart();

        webBuilder.Services.AddSingleton<ReindexGate>();
        webBuilder.Services.AddSingleton<GitHubWebhook>();
        webBuilder.Services.AddSingleton<IIssueDeliveries, IssueDeliveries>();
        webBuilder.Services.AddHostedService<AnsweringSwitchRefresh>();
        webBuilder.Services.AddSingleton<WebhookProcessing>();
        webBuilder.Services.AddHostedService<RetentionPurge>();
        webBuilder.Services.AddSingleton<IDocsGapInteractions, DocsGapInteractions>();
        webBuilder.Services.AddSingleton<IMaintainerChannel, DiscordMaintainerChannel>();
        webBuilder.Services.AddHostedService<DocsGapDigest>();
        webBuilder.Services.AddSingleton<IForumThreads, DiscordForumThreads>();
        webBuilder.Services.AddSingleton<IForumAnswerer, ForumAnswerer>();
        webBuilder.Services.AddHostedService<DelayedForumAnswers>();
        webBuilder.Services
            .AddDiscordGateway(options =>
            {
                options.Token = webBuilder.Configuration[ConfigurationPath.Combine(configSection, "Discord", "Token")];
                options.Intents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent;
            })
            .AddGatewayHandlers(typeof(Program).Assembly)
            .AddPrompterInteractions();

        var app = webBuilder.Build();

        await app.Services.GetRequiredService<IChunks>().EnsureSchema();

        app.AddModules(typeof(Program).Assembly);
        app.MapPrompterEndpoints();

        await app.RunAsync();
    },
    Console.Error);
