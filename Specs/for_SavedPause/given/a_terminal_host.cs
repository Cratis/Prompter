// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_SavedPause.given;

public class a_terminal_host : for_PaidWork.given.a_paid_work
{
    protected AnsweringSwitch Closed { get; private set; } = null!;
    protected IAnsweringPauses Pauses { get; private set; } = null!;
    protected PaidWork TerminalWork { get; private set; } = null!;

    protected async Task<Exception?> TryBeginWork() => await Catch.Exception(async () =>
    {
        await using var lease = await TerminalWork.BeginInteractive();
    });

    void Establish()
    {
        Closed = new(NullLogger<AnsweringSwitch>.Instance);
        Pauses = Substitute.For<IAnsweringPauses>();
        TerminalWork = new(Ledger, Microsoft.Extensions.Options.Options.Create(Options), Clock, Closed);
    }
}
