// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_LayaClassifier.when_classifying;

public class and_the_caller_cancels : given.a_classifier
{
    bool _cancelled;

    async Task Because()
    {
        transport.Delay = token => Task.Delay(Timeout.Infinite, token);
        using var cancellation = new CancellationTokenSource();
        var pending = classifier.Classify("Arc", [], cancellation.Token);
        await cancellation.CancelAsync();
        try { await pending; }
        catch (OperationCanceledException) { _cancelled = true; }
    }

    [Fact] void should_propagate_cancellation() => _cancelled.ShouldBeTrue();
}
