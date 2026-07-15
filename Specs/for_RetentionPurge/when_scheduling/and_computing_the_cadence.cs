// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_RetentionPurge.when_scheduling;

public class and_computing_the_cadence : Specification
{
    [Fact] void should_sweep_once_a_day() => RetentionPurge.Period.ShouldEqual(TimeSpan.FromDays(1));

    [Fact] void should_delay_the_first_sweep() => (RetentionPurge.InitialDelay > TimeSpan.Zero).ShouldBeTrue();

    [Fact]
    void should_purge_on_boot_rather_than_after_a_full_cadence() =>
        (RetentionPurge.InitialDelay < RetentionPurge.Period).ShouldBeTrue();
}
