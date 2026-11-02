// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using NetCord;
using NetCord.JsonModels;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace Cratis.Prompter.Specs.for_ActionButtons.when_nonpaid_actions_run;

public class and_acknowledgements_stall : given.an_action_interaction
{
    readonly List<CancellationToken> _tokens = [];
    bool _released;

    void Establish() => CreateContext(7);

    async Task Because()
    {
        Options.Protection.ActionClicksPerUser = 10;
        var config = Microsoft.Extensions.Options.Options.Create(Options);
        var clock = new ManualClock(Clock.GetUtcNow());
        var actions = new NonpaidActions(Admission, config, clock);
        var forward = new MaintainerForwardButtons(new(Client.Rest, Channel, Admission, config, clock, NullLogger<MaintainerForwards>.Instance), actions);
        var human = new HumanHandOffButton(new(Client.Rest, Admission, new(config, clock), config, clock, NullLogger<HumanHandOffs>.Instance), actions);
        Attach(forward);
        Attach(human);
        var first = forward.Documented(70, 90);
        var second = human.Handle(70, 90);
        var rejected = forward.Request(70, 90, "chronicle");
        await rejected;
        clock.Advance(TimeSpan.FromSeconds(5));
        await Task.WhenAll(first, second);
        await actions.Run(new(1, 70, 3), ButtonContext.Channel, new(Released, _ => Task.FromResult("done"), (_, _) => Task.CompletedTask, (_, _) => Task.CompletedTask), TestContext.Current.CancellationToken);
    }

    Task Released(CancellationToken token)
    {
        _released = true;
        return Task.CompletedTask;
    }

    void Attach(ComponentInteractionModule<ButtonInteractionContext> module)
    {
        var json = ((IJsonModel<JsonInteraction>)ButtonContext.Interaction).JsonModel;
        var interaction = new ButtonInteraction(json, ButtonContext.Guild, (_, _, _, _, token) => Stall(token), Client.Rest);
        SetContext(module, typeof(ComponentInteractionModule<ButtonInteractionContext>), new ButtonInteractionContext(interaction, Client));
    }

    async Task<InteractionCallbackResponse?> Stall(CancellationToken cancellationToken)
    {
        _tokens.Add(cancellationToken);
        await Task.Delay(Timeout.InfiniteTimeSpan, Clock, cancellationToken);
        return null;
    }

    [Fact] void should_not_queue_or_acknowledge_a_third_action() => _tokens.Count.ShouldEqual(2);
    [Fact] void should_cancel_both_acknowledgements() => _tokens.TrueForAll(token => token.IsCancellationRequested).ShouldBeTrue();
    [Fact] void should_do_no_downstream_discord_work() => Paths.Count.ShouldEqual(0);
    [Fact] void should_release_both_running_slots() => _released.ShouldBeTrue();
    [Fact] void should_not_reserve_paid_work() => NoPaidWork();
}
