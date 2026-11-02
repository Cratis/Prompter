// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Specs.Fakes;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_FeedbackProcessing.given;

public class a_feedback_processor : Specification
{
    protected PrompterOptions Options { get; } = new();
    protected ManualClock Clock { get; } = new(new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    protected IInteractionLog Log { get; } = Substitute.For<IInteractionLog>();
    protected FeedbackProcessing Processing { get; private set; } = null!;
    protected int Acknowledgements { get; private set; }
    protected int Repeats { get; private set; }
    protected int Refusals { get; private set; }

    protected void Create()
    {
        var options = Microsoft.Extensions.Options.Options.Create(Options);
        Processing = new(new(options, Clock), Log, options, Clock, NullLogger<FeedbackProcessing>.Instance);
    }

    protected Task Run(ulong user, long answer, CancellationToken token = default) =>
        Processing.Run(user, FeedbackButton.CustomId(FeedbackVerdict.Up, answer), new(Acknowledge, Repeat, Refuse), token);

    protected Task Acknowledge(CancellationToken token)
    {
        Acknowledgements++;
        return Task.CompletedTask;
    }

    protected Task Repeat(CancellationToken token)
    {
        Repeats++;
        return Task.CompletedTask;
    }

    protected Task Refuse(CancellationToken token)
    {
        Refusals++;
        return Task.CompletedTask;
    }
}
