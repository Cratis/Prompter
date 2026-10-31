// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Specs.Fakes;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_a_selection.given;

public class a_selection_reader : Specification
{
    CancellationTokenSource _cancellation = null!;
    CancellationTokenSource _deadline = null!;
    WorkLease _lease = null!;

    protected DateTimeOffset now { get; } = new(2026, 6, 5, 12, 0, 0, TimeSpan.Zero);
    protected ConversationLocation location { get; set; } = null!;
    protected IConversationHistory history { get; private set; } = null!;
    protected ConversationContextReader reader { get; private set; } = null!;
    protected IList<HistoryMessage> messages { get; } = new List<HistoryMessage>();
    protected Question result { get; set; } = null!;
    protected bool in_thread { get; set; } = true;

    protected Task<Question> Select(string current = "selected question")
    {
        _lease.Activate();
        return reader.ForSelection(current, location, in_thread);
    }

    void Establish()
    {
        location = new(1, 2, 100, now);
        history = Substitute.For<IConversationHistory>();
        history.IsPublic(Arg.Any<ConversationLocation>(), Arg.Any<CancellationToken>()).Returns(true);
        history.Before(Arg.Any<ConversationLocation>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IReadOnlyList<HistoryMessage>>(messages.ToArray()));
        reader = new(history, new ControlledTime(now));
        _cancellation = new();
        _deadline = new();
        _lease = new(Substitute.For<ISpendLedger>(), Guid.NewGuid(), new(1, 1), _cancellation, _deadline, () => { });
    }

    async Task Destroy() => await _lease.DisposeAsync();
}
