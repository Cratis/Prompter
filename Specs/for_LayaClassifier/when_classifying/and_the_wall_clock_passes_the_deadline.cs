// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Routing;

namespace Cratis.Prompter.Specs.for_LayaClassifier.when_classifying;

public class and_the_wall_clock_passes_the_deadline : given.a_classifier
{
    Task Because()
    {
        transport.Delay = _ => Task.Delay(TimeSpan.FromMilliseconds(1200));
        return Classify();
    }

    [Fact] void should_still_use_laya() => result.Source.ShouldEqual(ClassificationSource.Laya);
    [Fact] void should_select_bug_intent() => result.Intent.ShouldEqual(ClassificationIntent.Bug);
}
