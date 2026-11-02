// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Operations;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Prompter.Specs.for_WebhookProcessing.when_starting;

public class and_callbacks_fail_or_expire : Specification
{
    bool _afterFault;
    bool _expired;
    bool _afterExpiry;
    bool _shutdownDenied;

    void Because()
    {
        var options = new PrompterOptions();
        options.Protection.WebhookConcurrency = 1;
        var clock = new ManualClock(new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var processing = new WebhookProcessing(Microsoft.Extensions.Options.Options.Create(options), clock, NullLogger<WebhookProcessing>.Instance);
        processing.TryStart(token => Task.FromException(new OperationCanceledException()), default).ShouldBeTrue();
        _afterFault = processing.TryStart(token => Task.CompletedTask, default);
        processing.TryStart(
            token =>
            {
                clock.Advance(TimeSpan.FromSeconds(60));
                _expired = token.IsCancellationRequested;
                token.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            },
            default).ShouldBeTrue();
        _afterExpiry = processing.TryStart(token => Task.CompletedTask, default);
        _shutdownDenied = !processing.TryStart(token => Task.CompletedTask, new CancellationToken(true));
    }

    [Fact] void should_observe_faults_and_release_capacity() => _afterFault.ShouldBeTrue();
    [Fact] void should_cancel_after_sixty_seconds() => _expired.ShouldBeTrue();
    [Fact] void should_release_capacity_after_deadline_cancellation() => _afterExpiry.ShouldBeTrue();
    [Fact] void should_not_start_after_application_shutdown() => _shutdownDenied.ShouldBeTrue();
}
