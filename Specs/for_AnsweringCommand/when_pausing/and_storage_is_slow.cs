// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_AnsweringCommand.when_pausing;

public class and_storage_is_slow : given.an_answering_command
{
    readonly TaskCompletionSource _store = new();
    Task _running = Task.CompletedTask;
    bool _pausedBeforeStore;
    bool _deferredBeforeStore;
    bool _confirmedBeforeStore;

    void Establish() => Pauses.Set(true, Arg.Any<CancellationToken>()).Returns(_ => _store.Task);

    async Task Because()
    {
        Invoke(Guild, Role);
        _running = Command.Pause();
        await Task.Delay(200);
        _pausedBeforeStore = Switch.Paused;
        _deferredBeforeStore = Replied("\"type\":5");
        _confirmedBeforeStore = Replied("Answering paused.");
        _store.SetResult();
        await _running;
    }

    [Fact] void should_pause_before_the_write_completes() => _pausedBeforeStore.ShouldBeTrue();
    [Fact] void should_defer_before_the_write_completes() => _deferredBeforeStore.ShouldBeTrue();
    [Fact] void should_not_confirm_before_the_write_completes() => _confirmedBeforeStore.ShouldBeFalse();
    [Fact] void should_send_the_outcome_afterwards() => Replied("Answering paused.").ShouldBeTrue();
    [Fact] void should_send_the_outcome_as_a_followup() => Paths.Any(path => path.Contains("/webhooks/", StringComparison.Ordinal)).ShouldBeTrue();
}
