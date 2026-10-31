// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Specs.Fakes;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_PaidWork.given;

public class a_paid_work : Specification
{
    protected PaidWork Work { get; private set; } = null!;
    protected ISpendLedger Ledger { get; private set; } = null!;
    protected PrompterOptions Options { get; private set; } = null!;
    protected ControlledTime Clock { get; private set; } = null!;

    void Establish()
    {
        Options = new();
        Options.Protection.Anthropic.Model = Options.Anthropic.Model;
        Options.Protection.Anthropic.InputUsdPerMillion = 1;
        Options.Protection.Anthropic.OutputUsdPerMillion = 2;
        Options.Protection.Anthropic.MaxInputTokens = 32000;
        Options.Protection.Voyage.Model = Options.Voyage.Model;
        Options.Protection.Voyage.InputUsdPerMillion = 1;
        Options.Protection.Voyage.MaxInputTokens = 16000;
        Clock = new(new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Ledger = Substitute.For<ISpendLedger>();
        Ledger.Reserve(Arg.Any<SpendExposure>(), Arg.Any<CancellationToken>()).Returns(_ => (Guid?)Guid.NewGuid());
        Work = new(Ledger, Microsoft.Extensions.Options.Options.Create(Options), Clock);
    }

    void Destroy() => Work.Dispose();
}
