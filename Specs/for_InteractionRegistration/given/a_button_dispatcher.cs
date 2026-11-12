// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Routing;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.JsonModels;
using NetCord.Services;
using NetCord.Services.ComponentInteractions;
using NSubstitute;
using NSubstitute.Extensions;

namespace Cratis.Prompter.Specs.for_InteractionRegistration.given;

public class a_button_dispatcher : for_ActionButtons.given.an_action_interaction
{
    ServiceProvider _provider = null!;
    IInteractionCreateGatewayHandler _handler = null!;

    protected IComponentInteractionResultHandler<ButtonInteractionContext> Results { get; private set; } = null!;
    protected IInteractionLog Interactions { get; private set; } = null!;
    protected FeedbackVotes Votes { get; private set; } = null!;
    protected int DependencyResolutions { get; private set; }
    protected int Dispatches { get; private set; }

    protected async Task Dispatch(ulong? guild = 1, ulong? parent = 7)
    {
        foreach (var id in new[] { SuggestionButton.CustomId(0), ClarificationButton.CustomId(InteractionProduct.Chronicle), FeedbackButton.CustomId(FeedbackVerdict.Up, 42) })
        {
            await DispatchClick(id, guild, parent);
        }
    }

    protected async Task DispatchClick(string customId, ulong? guild = 1, ulong? parent = 7)
    {
        CreateContext(parent, customId);
        var json = ((IJsonModel<JsonInteraction>)ButtonContext.Interaction).JsonModel;
        json.GuildId = guild;
        if (guild is null)
        {
            json.Channel!.Type = ChannelType.DMChannel;
        }

        var interaction = new ButtonInteraction(json, guild is null ? null : ButtonContext.Guild, Response, Client.Rest);
        await _handler.HandleAsync(interaction);
        Dispatches++;
    }

    void Establish()
    {
        Results = Substitute.For<IComponentInteractionResultHandler<ButtonInteractionContext>>();
        Interactions = Substitute.For<IInteractionLog>();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddSingleton(Client);
        services.AddSingleton(Admission);
        var config = Microsoft.Extensions.Options.Options.Create(Options);
        services.AddSingleton(config);
        services.AddSingleton(Work);
        Votes = new(config, Clock);
        services.AddSingleton(Votes);
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<FeedbackProcessing>();
        services.AddSingleton(new ConversationContextReader(History, Clock));
        services.AddTransient(_ =>
        {
            DependencyResolutions++;
            return new ConversationAssistance(Substitute.For<IRequestClassifier>(), Answers, Substitute.For<IRelatedIssues>(), config);
        });
        services.AddTransient(_ =>
        {
            DependencyResolutions++;
            return Interactions;
        });
        services.AddPrompterInteractions();
        services.ConfigureComponentInteractions<ButtonInteraction, ButtonInteractionContext>(options =>
        {
            var registered = options.ResultHandler!;
            Results.ReturnsForAll(call => registered.HandleResultAsync(call.Arg<IExecutionResult>(), call.Arg<ButtonInteractionContext>(), call.Arg<GatewayClient?>(), call.Arg<ILogger>(), call.Arg<IServiceProvider>()));
            options.ResultHandler = Results;
        });
        _provider = services.BuildServiceProvider();
        _provider.GetRequiredService<ComponentInteractionService<ButtonInteractionContext>>().AddModules(typeof(Feedback).Assembly);
        var handlerType = services.Select(descriptor => descriptor.ServiceType).Single(type =>
            type.IsGenericType && type.GenericTypeArguments.Contains(typeof(ButtonInteractionContext)) &&
            typeof(IInteractionCreateGatewayHandler).IsAssignableFrom(type));
        _handler = (IInteractionCreateGatewayHandler)_provider.GetRequiredService(handlerType);
    }

    void Destroy() => _provider.Dispose();
}
