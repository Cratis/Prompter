// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Specs.Fakes;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.given;

public class a_history_reader : Specification
{
    protected static readonly DateTimeOffset Now = new(2026, 6, 5, 12, 0, 0, TimeSpan.Zero);
    CancellationTokenSource _deadline = null!;

    protected IConversationHistory history { get; private set; } = null!;
    protected ConversationContextReader reader { get; private set; } = null!;
    protected ConversationLocation location { get; } = new(1, 2, 100, Now);
    protected Question result { get; set; } = null!;
    protected CancellationTokenSource cancellation { get; private set; } = null!;
    protected WorkLease lease { get; private set; } = null!;

    protected static HistoryMessage Human(ulong id, string content = "Earlier question") =>
        new(id, 2, Now.AddMinutes(-1), content, IsBot: false, IsSelf: false, IsSystem: false);

    protected Task<Question> Read(string current = "What about that API?")
    {
        lease.Activate();
        return reader.For(current, location, cancellation.Token);
    }

    void Establish()
    {
        history = Substitute.For<IConversationHistory>();
        history.IsPublic(location, Arg.Any<CancellationToken>()).Returns(true);
        history.Before(location, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<HistoryMessage>>([]));
        reader = new(history, new ControlledTime(Now));
        cancellation = new();
        _deadline = new();
        lease = new(Substitute.For<ISpendLedger>(), Guid.NewGuid(), new(1, 1), cancellation, _deadline, () => { });
    }

    async Task Destroy() => await lease.DisposeAsync();
}
