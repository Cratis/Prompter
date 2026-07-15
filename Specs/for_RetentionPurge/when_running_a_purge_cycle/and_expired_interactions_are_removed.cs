// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_RetentionPurge.when_running_a_purge_cycle;

public class and_expired_interactions_are_removed : Specification
{
    IInteractionLog _log = null!;
    RetentionPurge _purge = null!;

    void Establish()
    {
        _log = Substitute.For<IInteractionLog>();
        _log.PurgeExpired(Arg.Any<CancellationToken>()).Returns(7);
        _purge = new RetentionPurge(_log, Options.Create(new PrompterOptions()), NullLogger<RetentionPurge>.Instance);
    }

    void Because() => _purge.PurgeOnce(CancellationToken.None).GetAwaiter().GetResult();

    [Fact] void should_purge_expired_interactions() => _log.Received(1).PurgeExpired(Arg.Any<CancellationToken>());
}
