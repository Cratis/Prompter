// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_RetentionPurge.when_running_a_purge_cycle;

public class and_the_interaction_log_fails : Specification
{
    IInteractionLog _log = null!;
    RetentionPurge _purge = null!;
    Exception _exception = null!;

    void Establish()
    {
        _log = Substitute.For<IInteractionLog>();
        _log.PurgeExpired(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new InvalidOperationException("database unavailable"));
        _purge = new RetentionPurge(_log, Options.Create(new PrompterOptions()), NullLogger<RetentionPurge>.Instance);
    }

    void Because()
    {
        try
        {
            _purge.PurgeOnce(CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            _exception = exception;
        }
    }

    [Fact] void should_swallow_the_failure_so_the_loop_survives() => _exception.ShouldBeNull();
    [Fact] void should_still_have_attempted_the_purge() => _log.Received(1).PurgeExpired(Arg.Any<CancellationToken>());
}
